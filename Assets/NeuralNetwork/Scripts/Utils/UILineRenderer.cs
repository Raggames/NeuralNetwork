﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic
{
    public UIGridRenderer grid;

    public Vector2Int gridSize;
    public List<Vector2> points;

    float width;
    float height;
    float unitWidth;
    float unitHeight;

    public float thickness = 10f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        width = rectTransform.rect.width;
        height = rectTransform.rect.height;

        unitWidth = width / (float)gridSize.x;
        unitHeight = height / (float)gridSize.y;

        if (points.Count < 2)
        {
            return;
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 point = points[i];

            float angle = 0f;

            angle = GetAngle(points[i], points[i + 1]) + 90f;
            DrawVerticesForPoint(point, vh, angle);

            Vector2 nextPoint = points[i + 1];
            DrawVerticesForPoint(nextPoint, vh, angle);
        }

        for (int i = 0; i < points.Count * 2 - 3; i++)
        {
            int index = i * 2;
            vh.AddTriangle(index + 0, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index + 0);
        }
    }

    public float GetAngle(Vector2 from, Vector2 to)
    {
        return (float)(Mathf.Atan2(unitHeight * (to.y - from.y), unitWidth * (to.x - from.x)) * Mathf.Rad2Deg);
    }

    void DrawVerticesForPoint(Vector2 point, VertexHelper vh, float angle)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);
    }

    private void Update()
    {
        if(grid != null)
        {
            if(gridSize != grid.gridSize)
            {
               gridSize = grid.gridSize;
                SetVerticesDirty();
            }
        }

        SetVerticesDirty();
    }
}