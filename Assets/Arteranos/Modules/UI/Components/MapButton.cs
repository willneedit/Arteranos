/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

//Attatch this script to a Button GameObject
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

/// <summary>
/// Resembles the 80`s World Wide Web Image Map button - sensitive to clicks, and tracks
/// _where_ the image has been clicked on. Additionally, click-and-move sends a stream of
/// coordinates, like on a laptop trackpad.
/// </summary>
public class MapButton : MonoBehaviour, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    public event Action<Vector2> OnClick;

    private bool buttonDown = false;

    private Vector2 PointerDataToRelativePos(PointerEventData eventData)
    {
        Vector2 clickPosition = eventData.position;
        RectTransform thisRect = transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            thisRect, clickPosition, Camera.main, out Vector2 result);

        // Normalize and clamp
        result = new(
            Mathf.Clamp01((result.x + thisRect.sizeDelta.x / 2) / thisRect.sizeDelta.x),
            Mathf.Clamp01((result.y + thisRect.sizeDelta.y / 2) / thisRect.sizeDelta.y)
            );

        return result;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonDown = true;

        // Implies the first submit of the coordinates, too.
        OnPointerMove(eventData);
    }

    public void OnPointerUp(PointerEventData eventData) 
        => buttonDown = false;

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!buttonDown) return;

        Vector2 wigdetPos = PointerDataToRelativePos(eventData);
        // Debug.Log($"{name} Click-and-move, Position: {wigdetPos}");
        OnClick?.Invoke(wigdetPos);
    }

}
