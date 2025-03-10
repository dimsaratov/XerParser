namespace XerParser
{
    /// <summary>
    ///  Parameters of the Xer element end-of-reading and conversion event Xer element
    /// </summary>
    /// <param name="xerElement">XerElement</param>
    /// <param name="time">TimeSpan</param>
    public class InitializingEventArgs(XerElement xerElement, TimeSpan time) : InitializeEventArgs(time)
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

}
