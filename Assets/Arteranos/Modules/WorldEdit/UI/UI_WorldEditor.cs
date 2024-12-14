/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;

namespace Arteranos.WorldEdit
{
    public class UI_WorldEditor : ActionPage
    {
        public WorldEditorUI WorldEditorUI;

        public override void Called(object data) => WorldEditorUI.Called(data);

        public override void OnEnterLeaveAction(bool onEnter) => WorldEditorUI.OnEnterLeaveAction(onEnter);
    }
}