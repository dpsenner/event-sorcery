using System;
using Npgsql;

namespace EventSorcery.Components.Historian
{
    internal static class NpgsqlCommandExtensions
    {
        public static NpgsqlCommand WithCommandText(this NpgsqlCommand command, string commandText)
        {
            command.CommandText = commandText;
            return command;
        }

        public static NpgsqlCommand AddParameter(this NpgsqlCommand command, string name, string value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;

            command.Parameters.Add(parameter);
            return command;
        }

        public static NpgsqlCommand AddParameter<T>(this NpgsqlCommand command, string name, T value)
            where T : struct
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;

            command.Parameters.Add(parameter);
            return command;
        }

        public static NpgsqlCommand AddParameter<T>(this NpgsqlCommand command, string name, T? value)
            where T : struct
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            if (value.HasValue)
            {
                parameter.Value = value;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }

            command.Parameters.Add(parameter);
            return command;
        }
    }
}
