using UnityEngine.SceneManagement;

namespace Orasyon.Story
{
    /// <summary>Carries "which story to play" into the Story scene.</summary>
    public static class StoryRouter
    {
        public const string StorySceneName = "Story";

        private static StoryScript pending;

        public static void Play(StoryScript script)
        {
            pending = script;
            SceneManager.LoadScene(StorySceneName);
        }

        /// <summary>Returns the requested story once (null if the Story scene was opened directly).</summary>
        public static StoryScript Consume()
        {
            var s = pending;
            pending = null;
            return s;
        }
    }
}
