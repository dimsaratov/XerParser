namespace XerParser
{
    /// <summary>
    ///  Parameters of the Xer element end-of-reading and conversion event Xer element
    /// </summary>
    /// <param name="xerElement">XerElement</param>
    public class InitializingEventArgs(XerElement xerElement) : InitializeEventArgs(xerElement.stopwatch.Elapsed)
    {
        /// <summary>
        /// 
        /// </summary>
        public XerElement XerElement { get; } = xerElement;

    }

    /// <summary>
    /// Parameters of the Xer element end-of-reading and conversion event Xer file
    /// </summary>
    /// <param name="time">TimeSpan</param>
    public class InitializeEventArgs(TimeSpan time) : EventArgs
    {
        /// <summary>
        /// The time interval after completing the reading and conversion of the Xer
        /// </summary>
        public TimeSpan Elapsed { get; private set; } = time;
    }

    /// <summary>
    ///  Parameters of the Xer element end-of-reading and conversion event Xer element
    /// </summary>
    /// <param name="xerElement">XerElement</param>
    /// <param name="isСompleted">The reading completed flag</param>
    public class ReadingEventArgs(XerElement xerElement, bool isСompleted) : InitializingEventArgs(xerElement)
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsCompleted { get; } = isСompleted;

    }
}
