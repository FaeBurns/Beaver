using UnityEngine;

namespace FFMpeg.Android
{
    public static class AndroidUtil
    {
        public static bool IsEnumMatch(AndroidJavaObject obj, string fullyQualifiedEnumName, string valueName)
        {
            AndroidJavaClass enumClass = new AndroidJavaClass(fullyQualifiedEnumName);
            AndroidJavaObject staticEnumField = enumClass.Get<AndroidJavaObject>(valueName);
            return AndroidJNI.IsSameObject(enumClass.GetRawObject(), staticEnumField.GetRawObject());
        }

        public static bool IsAnyEnumMatch(AndroidJavaObject obj, string fullyQualifiedEnumName, params string[] valueNames)
        {
            foreach (string valueName in valueNames)
            {
                if (IsEnumMatch(obj, fullyQualifiedEnumName, valueName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}