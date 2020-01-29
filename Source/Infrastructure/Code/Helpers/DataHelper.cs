using Lextm.SharpSnmpLib.Pipeline;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.Infrastructure.Helpers
{
    public class DataHelper : BaseHelper
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Copy the input object into the output object, iterating between all the properties, using the name as a match.
        /// </summary>
        /// <param name="ignoreNames">Property names ignored in the proccess.</param>
        public void CopyTo(Object inputObject, Object outputObject, IList<String> ignoreNames = null)
        {
            PropertyInfo[] inputProperties = inputObject.GetType().GetProperties();
            PropertyInfo[] outputProperties = outputObject.GetType().GetProperties();

            if (ignoreNames == null)
                ignoreNames = new List<String>();

            foreach (PropertyInfo inputProperty in inputProperties)
            {
                if (!inputProperty.CanRead)
                    continue;
                if (ignoreNames.Contains(inputProperty.Name))
                    continue;

                foreach (PropertyInfo outputProperty in outputProperties)
                {
                    if (!inputProperty.Name.Equals(outputProperty.Name))
                        continue;
                    if (!outputProperty.CanWrite)
                        continue;

                    Object value = inputProperty.GetValue(inputObject, null);

                    // Validate if posible null.
                    if (null == value
                        && outputProperty.PropertyType.IsGenericType
                        && outputProperty.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                        continue;

                    // Copy the info.
                    try
                    {
                        outputProperty.SetValue(
                            outputObject,
                            value,
                            null);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Improve this.
                        // Second try, for parsing base complex types, like unit to int.
                        try
                        {
                            outputProperty.SetValue(
                                outputObject,
                                Convert.ChangeType(value, outputProperty.PropertyType),
                                null);
                        }
                        catch (Exception ex2)
                        {
                            _logger.Error(ex2);
                            throw ex;
                        }
                    }
                }
            }            
        }

        /// <summary>
        /// Copy all the information selected in the selectedNames list, into the properties of the output object.
        /// Remember that the string must be the same as the name of the property to copy in BOTH objects.
        /// </summary>
        public void CopyToOnly(Object inputObject, Object outputObject, IList<String> selectedNames)
        {
            PropertyInfo[] inputProperties = inputObject.GetType().GetProperties();
            PropertyInfo[] outputProperties = outputObject.GetType().GetProperties();

            if (selectedNames == null)
                selectedNames = new List<String>();

            foreach (PropertyInfo inputProperty in inputProperties)
            {
                if (!inputProperty.CanRead)
                    continue;
                if (!selectedNames.Contains(inputProperty.Name))
                    continue;

                foreach (PropertyInfo outputProperty in outputProperties)
                {
                    if (!inputProperty.Name.Equals(outputProperty.Name))
                        continue;
                    if (!outputProperty.CanWrite)
                        continue;

                    Object value = inputProperty.GetValue(inputObject, null);

                    // Validate if posible null.
                    if (null == value
                        && outputProperty.PropertyType.IsGenericType
                        && outputProperty.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                        continue;

                    outputProperty.SetValue(
                        outputObject,
                        value,
                        null);
                }
            }
        }
    }
}
