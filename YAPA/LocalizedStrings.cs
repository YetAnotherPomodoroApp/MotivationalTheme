﻿
using YAPA.Localizations;

namespace YAPA
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static General _generalResources = new General();
        public General GeneralResources { get { return _generalResources; } }
    }
}