using System.Collections;
using System.Linq;
using UnityEngine;

namespace Testing
{
    public class TestExecutor : MonoBehaviour
    {
        [SerializeField] private float m_testLengthSeconds;

        [SerializeField] private PostProcessingEffectSource m_fxaaEffect;
        [SerializeField] private PostProcessingEffectSource m_colorChangeEffect;
        [SerializeField] private PostProcessingEffectSource m_fakeWorkEffect;

        public IEnumerator ExecuteTests()
        {
            // disable the objects as there may be multiple per obj
            DisableEffect(m_colorChangeEffect);
            DisableEffect(m_fxaaEffect);
            DisableEffect(m_fakeWorkEffect);

            // wait 10 seconds to allow stream to connect - should really receive an event from something for this but aaaaaaaaaaaaa
            // add 5 to account for the 5 second wait at start - does that even need to be there anymore???
            yield return new WaitForSeconds(15);

            // wait frame between tests
            yield return null;

            MarkTestBegun("AllOff");
            yield return WaitForCompletion();
            MarkTestEnded();

            yield return null;

            MarkTestBegun("AAOn");
            EnableEffect(m_fxaaEffect);
            yield return WaitForCompletion();
            DisableEffect(m_fxaaEffect);
            MarkTestEnded();

            yield return null;

            MarkTestBegun("ColorChange");
            EnableEffect(m_colorChangeEffect);
            yield return WaitForCompletion();
            DisableEffect(m_colorChangeEffect);
            MarkTestEnded();

            yield return null;

            yield return DoFakeWorkTest(1000);

            yield return null;

            Application.Quit();
        }

        private IEnumerator DoFakeWorkTest(int loopCount)
        {
            MarkTestBegun($"FakeWork_{loopCount}");
            EnableEffect(m_fakeWorkEffect);
            m_fakeWorkEffect.Parameters[0].IntValue = loopCount;
            yield return WaitForCompletion();
            DisableEffect(m_fakeWorkEffect);
            MarkTestEnded();
        }

        private IEnumerator WaitForCompletion()
        {
            yield return new WaitForSeconds(m_testLengthSeconds);
        }

        private void MarkTestBegun(string testName)
        {
            Tester.WriteToColumn("Log", $"Start {testName}");
            Debug.Log($"Starting test {testName}");
        }

        private void MarkTestEnded()
        {
            Tester.WriteToColumn("Log", "End");
            Debug.Log("Ending Test");
        }

        private void EnableEffect(PostProcessingEffectSource effect)
        {
            effect.gameObject.SetActive(true);
        }

        private void DisableEffect(PostProcessingEffectSource effect)
        {
            effect.gameObject.SetActive(false);
        }
    }
}