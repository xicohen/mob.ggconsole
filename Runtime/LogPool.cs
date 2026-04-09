#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace Mob404.Console
{
    /// <summary>
    /// Object pool cho log cells, tranh Instantiate/Destroy lien tuc
    /// </summary>
    public sealed class LogPool
    {
        private const int MaxCells = 300;
        private const int PrewarmCount = 20;

        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Transform _poolParent;
        private readonly Stack<LogCell> _available = new();
        private readonly List<LogCell> _active = new();

        public int ActiveCount => _active.Count;
        public IReadOnlyList<LogCell> ActiveCells => _active;

        public LogPool(GameObject prefab, Transform container, Transform poolParent)
        {
            _prefab = prefab;
            _container = container;
            _poolParent = poolParent;
        }

        public void Prewarm()
        {
            for (var i = 0; i < PrewarmCount; i++)
            {
                var cell = CreateCell();
                cell.ResetCell();
                cell.transform.SetParent(_poolParent, false);
                _available.Push(cell);
            }
        }

        /// <summary>
        /// Lay cell tu pool. Neu vuot MaxCells, recycle cell cu nhat.
        /// </summary>
        public LogCell Get()
        {
            if (_active.Count >= MaxCells && _active.Count > 0)
                Recycle(_active[0]);

            var cell = _available.Count > 0 ? _available.Pop() : CreateCell();

            cell.transform.SetParent(_container, false);
            cell.transform.SetAsLastSibling();
            _active.Add(cell);
            return cell;
        }

        /// <summary>
        /// Tra 1 cell ve pool
        /// </summary>
        public void Recycle(LogCell cell)
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

        private LogCell CreateCell()
        {
            var go = Object.Instantiate(_prefab);
            var cell = go.GetComponent<LogCell>();
            if (cell == null)
                throw new System.InvalidOperationException("PrefabTextLog missing LogCell component");
            return cell;
        }
    }
}
