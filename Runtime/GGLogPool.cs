#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace GGConsolePackage
{
    /// <summary>
    /// Object pool cho log cells, tranh Instantiate/Destroy lien tuc
    /// </summary>
    public sealed class GGLogPool
    {
        private const int MAX_CELLS = 300;
        private const int PREWARM_COUNT = 20;

        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Transform _poolParent;
        private readonly Stack<GGLogCell> _available = new();
        private readonly List<GGLogCell> _active = new();

        public int ActiveCount => _active.Count;
        public IReadOnlyList<GGLogCell> ActiveCells => _active;

        public GGLogPool(GameObject prefab, Transform container, Transform poolParent)
        {
            _prefab = prefab;
            _container = container;
            _poolParent = poolParent;
        }

        public void Prewarm()
        {
            for (var i = 0; i < PREWARM_COUNT; i++)
            {
                var cell = CreateCell();
                cell.ResetCell();
                cell.transform.SetParent(_poolParent, false);
                _available.Push(cell);
            }
        }

        /// <summary>
        /// Lay cell tu pool. Neu vuot MAX_CELLS, recycle cell cu nhat.
        /// </summary>
        public GGLogCell Get()
        {
            if (_active.Count >= MAX_CELLS && _active.Count > 0)
                Recycle(_active[0]);

            GGLogCell cell;
            if (_available.Count > 0)
            {
                cell = _available.Pop();
            }
            else
            {
                cell = CreateCell();
            }

            cell.transform.SetParent(_container, false);
            cell.transform.SetAsLastSibling();
            _active.Add(cell);
            return cell;
        }

        /// <summary>
        /// Tra 1 cell ve pool
        /// </summary>
        public void Recycle(GGLogCell cell)
        {
            _active.Remove(cell);
            cell.ResetCell();
            cell.transform.SetParent(_poolParent, false);
            _available.Push(cell);
        }

        /// <summary>
        /// Tra tat ca active cells ve pool
        /// </summary>
        public void RecycleAll()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var cell = _active[i];
                cell.ResetCell();
                cell.transform.SetParent(_poolParent, false);
                _available.Push(cell);
            }
            _active.Clear();
        }

        private GGLogCell CreateCell()
        {
            var go = Object.Instantiate(_prefab);
            var cell = go.GetComponent<GGLogCell>();
            if (cell == null)
                throw new System.InvalidOperationException("PrefabTextLog missing GGLogCell component");
            return cell;
        }
    }
}
