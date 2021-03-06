﻿using System;
using UnityEngine;

namespace MaterialColor.Helpers
{
    public static class MaterialHelper
    {
        public static SimHashes GetMaterialFromCell(int cellIndex)
        {
            if (!Grid.IsValidCell(cellIndex))
            {
                return SimHashes.Vacuum;
            }

            return TryCellIndexToSimHash(cellIndex);
        }

        private static SimHashes TryCellIndexToSimHash(int cellIndex)
        {
            try
            {
                return CellIndexToSimHash(cellIndex);
            }
            catch (Exception e)
            {
                State.Logger.Log("Cell or element index from index failed");
                State.Logger.Log(e);
            }

            return SimHashes.Vacuum;
        }

        private static SimHashes CellIndexToSimHash(int cellIndex)
        {
            var cell = Grid.Cell[cellIndex];

            var cellElementIndex = cell.elementIdx;
            var element = ElementLoader.elements[cellElementIndex];

            if (element != null)
            {
                return element.id;
            }
            else
            {
                State.Logger.Log("Element from cell failed.");
            }

            return SimHashes.Vacuum;
        }

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