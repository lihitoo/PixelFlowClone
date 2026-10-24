using System;
using Dreamy.Datasave;

namespace Dreamy.Template
{
    [Serializable]
    public sealed class TemplateSave : SaveData
    {
        public int DemoOpenCount;
        public int CurrentScore;
        public int BestScore;
        public bool IsInitialized;
    }
}
