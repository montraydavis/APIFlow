using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APIFlow.Endpoint
{
    public class APIFlowInputModel : IDictionary<string, IList<object>>
    {
        private IDictionary<string, IList<object>> inputs;

        public IList<object> this[string key] { get => inputs[key]; set => inputs[key] = value; }

        public ICollection<string> Keys => inputs.Keys;

        public ICollection<IList<object>> Values => inputs.Values;

        public int Count => inputs.Count;

        public bool IsReadOnly => false;

        public void Add(string key, IList<object> value)
        {
            inputs.Add(key, value);
        }

        public void Add(KeyValuePair<string, IList<object>> item)
        {
            inputs.Add(item);
        }

        public void Clear()
        {
            inputs.Clear();
        }

        public bool Contains(KeyValuePair<string, IList<object>> item)
        {
            return inputs.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return inputs.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, IList<object>>[] array, int arrayIndex)
        {
            inputs.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, IList<object>>> GetEnumerator()
        {
            return inputs.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return inputs.Remove(key);
        }

        public bool Remove(KeyValuePair<string, IList<object>> item)
        {
            return inputs.Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IList<object> value)
        {
            return inputs.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inputs.GetEnumerator();
        }

        public APIFlowInputModel()
        {
            inputs = new Dictionary<string, IList<object>>();
        }
    }
}
