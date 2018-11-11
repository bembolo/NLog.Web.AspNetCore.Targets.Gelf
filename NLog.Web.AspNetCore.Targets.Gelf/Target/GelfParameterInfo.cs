using NLog.Config;
using NLog.Layouts;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    /// <summary>
    /// Represents optional parameters to be included in the GELF message.
    /// </summary>
    [NLogConfigurationItem]
    public class GelfParameterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GelfParameterInfo" /> class.
        /// </summary>
        public GelfParameterInfo()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GelfParameterInfo" /> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterLayout">The parameter layout.</param>
        internal GelfParameterInfo(string parameterName, Layout parameterLayout)
        {
            Name = parameterName;
            Layout = parameterLayout;
        }

        /// <summary>
        /// Gets or sets the GELF parameter name.
        /// </summary>
        /// <docgen category='Parameter Options' order='10' />
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the layout that should be use to calcuate the value for the parameter.
        /// </summary>
        /// <docgen category='Parameter Options' order='10' />
        [RequiredParameter]
        public Layout Layout { get; set; }
    }
}