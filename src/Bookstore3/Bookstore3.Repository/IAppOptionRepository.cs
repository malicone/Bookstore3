using Bookstore3.Model;
using KpzRepository.Repository;

namespace Bookstore3.Repository;

public interface IAppOptionRepository : IKpzRepository<string, app_option>
{
    /// <summary>
    /// Returns the value of the option as a string. If the option does not exist, returns null.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <returns>The value of the option as a string, or null if the option does not exist.</returns>
    string? GetOptionAsString(string optionName);

    /// <summary>
    /// Returns the value of the option as a long. If the option does not exist or cannot be parsed as a long, returns null.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <returns>The value of the option as a long, or null if the option does not exist or cannot be parsed as a long.</returns>
    long? GetOptionAsLong(string optionName);
    /// <summary>
    /// Returns the value of the option as a boolean. If the option does not exist or cannot be parsed as a boolean, returns null.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <returns>The value of the option as a boolean, or null if the option does not exist or cannot be parsed as a boolean.</returns>
    bool? GetOptionAsBool(string optionName);

    /// <summary>
    /// Returns the value of the option as a double. If the option does not exist or cannot be parsed as a double, returns null.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <returns>The value of the option as a double, or null if the option does not exist or cannot be parsed as a double.</returns>
    double? GetOptionAsDouble(string optionName);

    /// <summary>
    /// Sets the value of the option as a string. If the option does not exist, it will be created. If the option already exists, its value will be updated.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <param name="optionValue">The value to set for the option.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    bool SetOptionAsString(string optionName, string? optionValue);    

    /// <summary>
    /// Sets the value of the option as a boolean. If the option does not exist, it will be created. If the option already exists, its value will be updated.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <param name="optionValue">The value to set for the option.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    bool SetOptionAsBool(string optionName, bool? optionValue);

    /// <summary>
    /// Sets the value of the option as a long. If the option does not exist, it will be created. If the option already exists, its value will be updated.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <param name="optionValue">The value to set for the option.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    bool SetOptionAsLong(string optionName, long? optionValue);

    /// <summary>
    /// Sets the value of the option as a double. If the option does not exist, it will be created. If the option already exists, its value will be updated.
    /// </summary>
    /// <param name="optionName">The name of the option. Every option in the system has a unique name.</param>
    /// <param name="optionValue">The value to set for the option.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    bool SetOptionAsDouble(string optionName, double? optionValue);
}