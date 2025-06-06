namespace XerParser
{
    internal class ParsingTables()
    {
        private readonly List<string> _list = [];
        private readonly ReaderWriterLockSlim _lock = new();

        public void Push(string item)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Insert(0, item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(string item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryPop(out string item)
        {
            item = default;
            try
            {
                _lock.EnterWriteLock();
                if (_list.Count == 0)
                {
                    return false;
                }
                item = _list[0];
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}