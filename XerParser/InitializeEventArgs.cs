namespace XerParser
{
    /// <summary>
    ///  Parameters of the Xer element end-of-reading and conversion event Xer element
    /// </summary>
    /// <param name="xerElement">XerElement</param>
    public class InitializedEventArgs(XerElement xerElement) : InitializingEventArgs(xerElement.stopwatch.Elapsed)
    {
        /// <summary>
        /// XerElement
        /// </summary>
        public XerElement XerElement { get; } = xerElement;
    }

    /// <summary>
    /// Parameters of the Xer element end-of-reading and conversion event Xer file
    /// </summary>
    /// <param name="time">TimeSpan</param>
    public class InitializingEventArgs(TimeSpan time) : EventArgs
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
    public class ReadedEventArgs(XerElement xerElement, bool isСompleted) : InitializedEventArgs(xerElement)
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsCompleted { get; } = isСompleted;

    }
}
