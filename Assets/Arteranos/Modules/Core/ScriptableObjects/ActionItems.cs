/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Arteranos.Core
{
    public interface IActionPage : IMonoBehaviour
    {
        void Back(object result);
        void Call(object data);
        bool CanBeCalled(object data);
        void OnEnterLeaveAction(bool onEnter);
    }

    public class ActionPage : UIBehaviour, IActionPage 
    { 
        public virtual bool CanBeCalled(object data) { return true; }

        public virtual void Call(object data) { }

        public virtual void Back(object result) { ActionRegistry.Back(result); }

        public virtual void OnEnterLeaveAction(bool onEnter)
        { 
            gameObject.SetActive(onEnter);
        }
    }

    [Serializable]
    public struct ActionItem
    {
        public string Id;
        public ActionPage ActionGameObject;
    }

    [CreateAssetMenu(fileName = "ActionItems", menuName = "Scriptable Objects/Application/Action Items")]
    public class ActionItems : ScriptableObject
    {
        public ActionItem[] Items;
    }

    public static class ActionRegistry
    {
        private struct StackItem
        {
            public IActionPage caller;
            public bool floating;
            public Action<object> callback;
        }

        private static Stack<StackItem> _actionStack = new();
        private static Dictionary<string, IActionPage> _actionRegistry = new();

        public static void Register(ActionItems items)
        {
            foreach (ActionItem item in items.Items) _actionRegistry.Add(
                item.Id, 
                item.ActionGameObject);
        }
             
        public static bool CanCall(string actionId, object data = null, IActionPage callTo = null)
        {
            if(callTo != null)
                return callTo.CanBeCalled(data);

            IActionPage action = GetAction(actionId);
            return action.CanBeCalled(data);
        }

        public static GameObject Call(string actionId, object data = null, IActionPage callTo = null, Action<object> callback = null)
        {
            if (!CanCall(actionId, data, callTo)) return null;

            IActionPage page = GetAction(actionId);

            StackItem newItem = new()
            {
                floating = callTo == null,
                callback = callback
            };

            if (newItem.floating)
            {
                if (page == null) throw new ArgumentNullException(nameof(callTo));

                GameObject go = Object.Instantiate(page.gameObject);
                newItem.caller = go.GetComponent<IActionPage>();
            }
            else
            {
                newItem.caller = callTo;
                newItem.caller.OnEnterLeaveAction(true);
            }

            if (_actionStack.TryPeek(out StackItem oldItem))
            {
                if (oldItem.caller as Object)
                    oldItem.caller.OnEnterLeaveAction(false);
                else
                {
                    Debug.LogWarning("Destroyed object in action stack");
                    _actionStack.Pop();
                }
            }

            _actionStack.Push(newItem);

            newItem.caller.Call(data);

            return newItem.caller.gameObject;
        }

        public static void Back(object result = null)
        {
            while(_actionStack.TryPop(out StackItem item))
            {
                if (!(item.caller as Object))
                {
                    // Invalidate result because it loses its sense.
                    result = null;
                    continue;
                }

                if (item.floating) Object.Destroy(item.caller.gameObject);
                else item.caller.OnEnterLeaveAction(false);

                item.callback?.Invoke(result);
                break;
            }

            if(_actionStack.TryPeek(out StackItem lowerItem))
                lowerItem.caller?.OnEnterLeaveAction(true);
        }

        public static void Drop()
        {
            while(_actionStack.Count > 0) Back(null);
        }

        private static IActionPage GetAction(string actionId)
        {
            if (!_actionRegistry.ContainsKey(actionId))
                throw new ArgumentException($"Action not registered: {actionId}");

            return _actionRegistry[actionId];
        }
    }
}