﻿using System;
using UnityEngine;

namespace MaterialColor.Helpers
{
    public static class MaterialHelper
    {
        public static SimHashes ExtractMaterial(Component component)
        {
            var primaryElement = component.GetComponent<PrimaryElement>();

            if (primaryElement != null)
            {
                return primaryElement.ElementID;
            }
            else
            {
                State.Logger.Log("PrimaryElement not found in: " + component);
                return SimHashes.Vacuum;
            }
        }
    }
}