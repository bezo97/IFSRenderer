using System;
using System.Collections.Generic;
using Cloo;
using System.Collections;

namespace IFSEngine
{
    /// <summary>
    /// ez azert kell, mert a cloo maga hozza letre az eventeket,
    /// es igy nem biztos hogy fel tudunk iratkozni rajuk mielott bekovetkeznek.
    /// +automatikus resource felszabaditas remove-nal
    /// </summary>
    internal class CLEventCollection : ICollection<ComputeEventBase>
    {
        private List<ComputeEventBase> l = new List<ComputeEventBase>();
        private ComputeCommandStatusChanged computeFrame_Finished;
        #region List
        public int Count => l.Count;
        public bool IsReadOnly => false;
        public void Clear() => l.Clear();
        public bool Contains(ComputeEventBase item) => l.Contains(item);
        public void CopyTo(ComputeEventBase[] array, int arrayIndex) => l.CopyTo(array, arrayIndex);
        public IEnumerator<ComputeEventBase> GetEnumerator() => l.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => l.GetEnumerator();
        #endregion

        public void Add(ComputeEventBase item)
        {
            //lenyeg:
            item.Completed += computeFrame_Finished;
            item.Aborted += computeFrame_Finished;//TODO: log abort
            l.Add(item);
        }

        public bool Remove(ComputeEventBase item)
        {
            //TODO: log item start/submit/finish time
            try
            {
                item.Dispose();
                l.Remove(item);
            }
            catch { /*megesik*/ }
            return true;
        }

        public CLEventCollection(ComputeCommandStatusChanged ComputeFrame_Finished)
        {
            computeFrame_Finished = ComputeFrame_Finished;
        }

    }
}
