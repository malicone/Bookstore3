using Bookstore3.Model;
using Dapper;
using KpzRepository.Repository;
using System.Data;

namespace Bookstore3.Repository;

public class AppOptionRepository : KpzRepository<string, app_option>, IAppOptionRepository
{
    public AppOptionRepository(IDbConnection connection) : base(connection)
    {

    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.GetOptionAsBool"/>
    /// </summary>
    public bool? GetOptionAsBool(string optionName)
    {
        string? optionAsString = GetOptionAsString(optionName);
        if (bool.TryParse(optionAsString, out bool result))
        {
            return result;
        }
        return null;
    }
     
    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.GetOptionAsDouble"/>
    /// </summary>
    public double? GetOptionAsDouble(string optionName)
    {
        string? optionAsString = GetOptionAsString(optionName);
        if(double.TryParse(optionAsString, out double result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.GetOptionAsLong"/>
    /// </summary>
    public long? GetOptionAsLong(string optionName)
    {
        string? optionAsString = GetOptionAsString(optionName);
        if (long.TryParse(optionAsString, out long result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.GetOptionAsString"/>
    /// </summary>
    public string? GetOptionAsString(string optionName)
    {
        if(OpenConnection())
        {
            app_option? option = Get(optionName);
            if(option == null)
                return null;

            return option.option_value;
        }
        return null;
    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.SetOptionAsBool"/>
    /// </summary>
    public bool SetOptionAsBool(string optionName, bool? optionValue)
    {
        return SetOptionAsString(optionName, optionValue?.ToString());
    }

    public bool SetOptionAsDouble(string optionName, double? optionValue)
    {
        return SetOptionAsString(optionName, optionValue?.ToString());
    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.SetOptionAsLong"/>
    /// </summary>
    public bool SetOptionAsLong(string optionName, long? optionValue)
    {
        return SetOptionAsString(optionName, optionValue?.ToString());
    }

    /// <summary>
    /// <inheritdoc cref="IAppOptionRepository.SetOptionAsString"/>
    /// </summary>
    public bool SetOptionAsString(string optionName, string? optionValue)
    {
        if(OpenConnection())
        {
            if(Exists(optionName))
            {
                app_option optionToUpdate = new app_option
                {
                    option_name = optionName,
                    option_value = optionValue
                };
                return Update(optionToUpdate);
            }
            else
            {
                app_option optionToInsert = new app_option
                {
                    option_name = optionName,
                    option_value = optionValue
                };
                return Add(optionToInsert);
            }
        }
        return false;
    }
}