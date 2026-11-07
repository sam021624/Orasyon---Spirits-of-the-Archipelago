using System;
using UnityEngine;

namespace Orasyon.Story
{
    public enum Speaker
    {
        Narrator,
        Albularyo,
        Lola
    }

    [Serializable]
    public sealed class StoryLine
    {
        public Speaker speaker;
        [TextArea(2, 4)] public string text;

        [Tooltip("Flash the screen white before this line (e.g. the book's magic).")]
        public bool flash;
        [Tooltip("Optional: switch to this background during the flash (e.g. being pulled into the book).")]
        public Texture2D changeBackground;
        [Tooltip("Lola is standing in the scene. Off = she speaks through the book.")]
        public bool lolaOnStage = true;
    }

    /// <summary>
    /// One dialogue scene (prologue, chapter interlude, ...). Edit the lines in the Inspector.
    /// Played by StoryPlayer in the Story scene; afterwards <see cref="nextScene"/> is loaded.
    /// </summary>
    [CreateAssetMenu(menuName = "Orasyon/Story Script", fileName = "NewStory")]
    public sealed class StoryScript : ScriptableObject
    {
        [Header("Title Card")]
        public string title;
        public string subtitle;

        [Header("Scene")]
        public Texture2D   background;
        public StoryLine[] lines;

        [Tooltip("Scene to load when the dialogue ends (must be in Build Settings).")]
        public string nextScene = "Level1";
    }
}
