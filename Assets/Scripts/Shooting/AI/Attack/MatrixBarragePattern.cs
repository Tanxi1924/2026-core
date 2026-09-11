using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "TwinBody/Barrage/Matrix Pattern", fileName = "MatrixPattern")]
public class MatrixBarragePattern : ScriptableObject
{
    [SerializeField, Range(1, 25)] private int rows = 5;
    [SerializeField, Range(1, 25)] private int columns = 5;
    [SerializeField, HideInInspector] private bool[] cells = new bool[25];
    [Min(0.01f)] public float HorizontalSpacing = 0.6f;
    [Min(0.01f)] public float VerticalSpacing = 0.6f;
    [Tooltip("Rotates the shape only, not bullet movement. Degrees counterclockwise.")]
    public float ShapeAngle;
    public int Rows => rows;
    public int Columns => columns;
    public bool IsOn(int row, int column) => cells != null && row >= 0 && row < rows
        && column >= 0 && column < columns && row * columns + column < cells.Length
        && cells[row * columns + column];

    public void SetCell(int row, int column, bool value)
    {
        if (row >= 0 && row < rows && column >= 0 && column < columns)
            cells[row * columns + column] = value;
    }

    public void Resize(int newRows, int newColumns)
    {
        newRows = Mathf.Clamp(newRows, 1, 25);
        newColumns = Mathf.Clamp(newColumns, 1, 25);
        var next = new bool[newRows * newColumns];
        for (int r = 0; r < Mathf.Min(rows, newRows); r++)
            for (int c = 0; c < Mathf.Min(columns, newColumns); c++)
                next[r * newColumns + c] = IsOn(r, c);
        rows = newRows;
        columns = newColumns;
        cells = next;
    }

    public Vector3 CellOffset(int row, int column)
    {
        Vector3 offset = new Vector3((column - (columns - 1) * 0.5f) * HorizontalSpacing,
            ((rows - 1) * 0.5f - row) * VerticalSpacing, 0f);
        return Quaternion.Euler(0f, 0f, ShapeAngle) * offset;
    }

    public void GetOffsets(List<Vector3> result)
    {
        result.Clear();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (IsOn(r, c)) result.Add(CellOffset(r, c));
    }

    public enum Preset { Empty, Full, Diamond, HollowDiamond, Cross, Frame }
    public void Fill(Preset preset)
    {
        float middleRow = (rows - 1) * 0.5f;
        float middleColumn = (columns - 1) * 0.5f;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                bool inside = InDiamond(r, c);
                bool edge = inside && (!InDiamond(r - 1, c) || !InDiamond(r + 1, c)
                    || !InDiamond(r, c - 1) || !InDiamond(r, c + 1));
                bool enabled = preset == Preset.Full
                    || (preset == Preset.Diamond && inside)
                    || (preset == Preset.HollowDiamond && edge)
                    || (preset == Preset.Cross && (Mathf.Abs(r - middleRow) <= 0.5f
                        || Mathf.Abs(c - middleColumn) <= 0.5f))
                    || (preset == Preset.Frame && (r == 0 || r == rows - 1 || c == 0 || c == columns - 1));
                SetCell(r, c, enabled);
            }
    }

    private bool InDiamond(int r, int c)
    {
        if (r < 0 || c < 0 || r >= rows || c >= columns) return false;
        float ry = (rows - 1) * 0.5f;
        float rx = (columns - 1) * 0.5f;
        float y = ry == 0f ? 0f : Mathf.Abs(r - ry) / ry;
        float x = rx == 0f ? 0f : Mathf.Abs(c - rx) / rx;
        return x + y <= 1.00001f;
    }

    public void Mirror(bool horizontal)
    {
        var next = new bool[rows * columns];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                next[r * columns + c] = horizontal ? IsOn(r, columns - 1 - c) : IsOn(rows - 1 - r, c);
        cells = next;
    }

    private void OnValidate()
    {
        rows = Mathf.Clamp(rows, 1, 25);
        columns = Mathf.Clamp(columns, 1, 25);
        if (cells == null || cells.Length != rows * columns) Resize(rows, columns);
        HorizontalSpacing = Mathf.Max(0.01f, HorizontalSpacing);
        VerticalSpacing = Mathf.Max(0.01f, VerticalSpacing);
    }
}

