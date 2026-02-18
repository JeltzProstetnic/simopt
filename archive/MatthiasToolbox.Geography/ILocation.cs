using System;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// an interface for geographical items which are identifiable,
    /// named and have a geographic location (latitude and longitude)
	/// </summary>
	public interface ILocation
	{
        /// <summary>
        /// Name of the ILocation
        /// </summary>
		string Name { get; }

        /// <summary>
        /// a unique identifier
        /// </summary>
		int ID { get; }

        /// <summary>
        /// Latitude of the ILocation
        /// </summary>
		double Latitude {get; set;}

        /// <summary>
        /// Longitude of the ILocation
        /// </summary>
		double Longitude {get; set;}
	}
}
