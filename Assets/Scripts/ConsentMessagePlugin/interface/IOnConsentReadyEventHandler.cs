﻿using UnityEngine.EventSystems;

namespace GdprConsentLib
{
    public interface IOnConsentReadyEventHandler : IConsentEventHandler
    {
        void OnConsentReady(SpConsents consents);
    }
}