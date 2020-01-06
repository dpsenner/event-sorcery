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

        public static NpgsqlCommand AddParameter<T>(this NpgsqlCommand command, string name, T value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;

            command.Parameters.Add(parameter);
            return command;
        }
    }
}