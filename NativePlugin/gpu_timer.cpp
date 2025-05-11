#include <vulkan/vulkan.h>
#include "IUnityLog.h"
#include "IUnityInterface.h"
#include "IUnityGraphics.h"
#include "IUnityGraphicsVulkan.h"

#ifdef _WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility("default")))
#endif

// for logging
#include <cstdarg>
#include <cstdio>

static IUnityLog* s_UnityLog = nullptr;
static IUnityInterfaces* s_UnityInterfaces = nullptr;
static IUnityGraphicsVulkan* s_UnityVulkan = nullptr;
static VkDevice s_Device = VK_NULL_HANDLE;
static VkQueryPool s_QueryPool = VK_NULL_HANDLE;

static float timestampPeriod;

const int MAX_TIMERS = 64;
static bool timerInUse[MAX_TIMERS] = {false};

// Map ID to query index (2 per ID: start and end)
inline int startQueryIndex(int id) { return id * 2; }
inline int endQueryIndex(int id) { return id * 2 + 1; }

static void LogUnity(const char* msg, ...)
{
    return;
    if (s_UnityLog)
    {
        // Create a buffer to hold the formatted message
        char buffer[1024];  // Adjust size as needed

        // Start the variable argument list
        va_list args;
        va_start(args, msg);

        // Format the message into the buffer
        vsnprintf(buffer, sizeof(buffer), msg, args);

        // End the variable argument list
        va_end(args);

        // Log the formatted message
        UNITY_LOG(s_UnityLog, buffer);
    }
}

// gets vulkan instance on initialization
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    if (eventType == kUnityGfxDeviceEventInitialize)
    {
        IUnityGraphics* graphics = s_UnityInterfaces->Get<IUnityGraphics>();
        UnityGfxRenderer renderer = graphics->GetRenderer();
        if (renderer == kUnityGfxRendererVulkan)
            LogUnity("Beaver::OnGraphicsDeviceEvent::Vulkan");
        else
            LogUnity("Beaver::OnGraphicsDeviceEvent::Other");

        s_UnityVulkan = s_UnityInterfaces->Get<IUnityGraphicsVulkan>();
    }
    else if (eventType == kUnityGfxDeviceEventShutdown)
    {
        s_UnityVulkan = nullptr;
    }
}

// ENTRY POINT
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
	s_UnityLog = unityInterfaces->Get<IUnityLog>();
    IUnityGraphics* graphics = unityInterfaces->Get<IUnityGraphics>();
    graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

// EXIT POINT
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    s_UnityInterfaces->Get<IUnityGraphics>()->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    if (s_Device != VK_NULL_HANDLE && s_QueryPool != VK_NULL_HANDLE)
    {
        vkDestroyQueryPool(s_Device, s_QueryPool, nullptr);
        s_QueryPool = VK_NULL_HANDLE;
    }
}

extern "C" bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitGpuTimer()
{
	// if state is invalid then fail
    if (!s_UnityVulkan)
    {
        UNITY_LOG_ERROR(s_UnityLog, "Beaver::TryInitGpuTimer UnityVulkan instance not set");
        return false;
    }
    if (s_QueryPool != VK_NULL_HANDLE)
    {
        UNITY_LOG_WARNING(s_UnityLog, "Beaver::TryInitGpuTimer query pool already initialized");
        return true;
    }

    // get vulkan device
    UnityVulkanInstance vkInstance = s_UnityVulkan->Instance();
    s_Device = vkInstance.device;
    
	if (!s_Device)
    {
        UNITY_LOG_ERROR(s_UnityLog, "Beaver::TryInitGpuTimer Failed to get vk device");
		return false;
    }

    // get timestamp period from device info
    VkPhysicalDeviceProperties props{};
    vkGetPhysicalDeviceProperties(vkInstance.physicalDevice, &props);
    timestampPeriod = props.limits.timestampPeriod;

	VkQueryPoolCreateInfo queryInfo = {};
    queryInfo.sType = VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO;
    queryInfo.queryType = VK_QUERY_TYPE_TIMESTAMP;
    queryInfo.queryCount = MAX_TIMERS * 2;

	auto result = vkCreateQueryPool(s_Device, &queryInfo, nullptr, &s_QueryPool);
	if (result != VK_SUCCESS)
    {
        UNITY_LOG_ERROR(s_UnityLog, "Beaver::TryInitGpuTimer Failed to create query pool");
		return false;
    }
	else{
        // use direct to bypass log disable
        UNITY_LOG(s_UnityLog, "Beaver::TryInitGpuTimer::Successfully created query pool");
	}
	return true;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API BeginRecordTiming(int id)
{
    // silent fail if invalid
    LogUnity("Beaver::BeginRecordingTiming{%d}", id);
    if (!s_UnityVulkan || id < 0 || id >= MAX_TIMERS) return;

    LogUnity("Beaver::BeginRecordingTiming::CommandRecordingState");
    UnityVulkanRecordingState recordingState = {};
    s_UnityVulkan->CommandRecordingState(&recordingState, kUnityVulkanGraphicsQueueAccess_DontCare);

    if (recordingState.commandBuffer == VK_NULL_HANDLE)
    {
        LogUnity("Beaver::BeginRecordingTiming::BadCommandBuffer");
        return;
    }

    if (s_QueryPool == VK_NULL_HANDLE){
        LogUnity("Beaver::BeginRecordingTiming::BadQueryPool");
        return;
    }

    LogUnity("Beaver::BeginRecordingTiming::ResetQueryPool");
    vkCmdResetQueryPool(recordingState.commandBuffer, s_QueryPool, startQueryIndex(id), 2);
    LogUnity("Beaver::BeginRecordingTiming::WriteTimestamp");
    vkCmdWriteTimestamp(recordingState.commandBuffer, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, s_QueryPool, startQueryIndex(id));

    timerInUse[id] = true;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API EndRecordTiming(int id)
{
    // silent fail if invalid
    LogUnity("Beaver::EndRecordingTiming{%d}", id);
    if (!s_UnityVulkan || id < 0 || id >= MAX_TIMERS || !timerInUse[id]) return;

    LogUnity("Beaver::EndRecordingTiming::CommandRecordingState");
    UnityVulkanRecordingState recordingState = {};
    s_UnityVulkan->CommandRecordingState(&recordingState, kUnityVulkanGraphicsQueueAccess_DontCare);

    if (recordingState.commandBuffer == VK_NULL_HANDLE)
    {
        LogUnity("Beaver::EndRecordingTiming::BadCommandBuffer");
        return;
    }

    if (s_QueryPool == VK_NULL_HANDLE){
        LogUnity("Beaver::EndRecordingTiming::BadQueryPool");
        return;
    }

    LogUnity("Beaver::EndRecordingTiming::WriteTimestamp");
    vkCmdWriteTimestamp(recordingState.commandBuffer, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, s_QueryPool, endQueryIndex(id));
}

extern "C" double UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetElapsedNanosecondsForId(int id)
{
    // silent fail if invalid
    LogUnity("Beaver::GetElapsedNanosecondsForId{%d}", id);
    if (!timerInUse[id] || id < 0 || id >= MAX_TIMERS) return 0;
    
	LogUnity("Beaver::GetElapsedNanosecondsForId::QueryStart");
    uint64_t start = 0, end = 0;
    vkGetQueryPoolResults(s_Device, s_QueryPool, startQueryIndex(id), 1, sizeof(uint64_t), &start, sizeof(uint64_t),
                          VK_QUERY_RESULT_64_BIT | VK_QUERY_RESULT_WAIT_BIT);

    LogUnity("Beaver::GetElapsedNanosecondsForId::QueryEnd");
    vkGetQueryPoolResults(s_Device, s_QueryPool, endQueryIndex(id), 1, sizeof(uint64_t), &end, sizeof(uint64_t),
                          VK_QUERY_RESULT_64_BIT | VK_QUERY_RESULT_WAIT_BIT);

    if (end <= start)
        return 0;

    double elapsedNs = (end - start) * timestampPeriod;
    return elapsedNs;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnRenderEventWithData(int eventID, void* data)
{
    // idk but apparently cross-platform interop to use intptr
    intptr_t idPtr = reinterpret_cast<intptr_t>(data);
    int id = static_cast<int>(idPtr);
    switch(eventID){
        case 1:
            BeginRecordTiming(id);
            break;
        case 2:
            EndRecordTiming(id);
            break;
    }
}

extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT GetRenderEventFunc(){
    return OnRenderEventWithData;
}