/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.UI;
using System;

namespace Arteranos.WorldEdit
{
    public class NewObjectPicker : ActionPage
    {
        public APChoiceBook ChoiceBook;

        public override void OnEnterLeaveAction(bool onEnter)
        {
            base.OnEnterLeaveAction(onEnter);

            NewObjectPanel[] NewObjectPanels = ChoiceBook.PaneList.GetComponentsInChildren<NewObjectPanel>(true);
            foreach (NewObjectPanel panel in NewObjectPanels)
            {
                if (onEnter) panel.OnAddingNewObject += ToReturn;
                else panel.OnAddingNewObject -= ToReturn;
            }
        }

        private void ToReturn(WorldObjectInsertion insertion) => BackOut(insertion);
    }
}