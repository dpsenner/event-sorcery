using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;

namespace EventSorcery.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection serviceCollection, string[] arguments)
        {
            return serviceCollection
                .AddSingleton(serviceProvider => new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddEnvironmentVariables()
                    .AddCommandLine(arguments)
                    .AddJsonFiles(serviceProvider)
                    .Build());
        }

        public static IServiceCollection AddConfigurationPath(this IServiceCollection serviceCollection, string path)
        {
            return serviceCollection
                .AddTransient<IConfigurationPathProvider>(serviceProvider => new Internals.ConfigurationPathProvider()
                {
                    Path = path,
                });
        }

        public static IServiceCollection AddConfigurationBinding<T>(this IServiceCollection serviceCollection)
            where T : class
        {
            return serviceCollection
                .AddTransient(serviceProvider => serviceProvider.GetRequiredService<IConfigurationRoot>().Get<T>());
        }

        public static IServiceCollection AddConfigurationBinding<T>(this IServiceCollection serviceCollection, params string[] sectionKeys)
            where T : class, new()
        {
            return serviceCollection.AddConfigurationBinding<T>((serviceProvider, t) => { }, sectionKeys);
        }

        public static IServiceCollection AddConfigurationBinding<T>(this IServiceCollection serviceCollection, Action<IServiceProvider, T> configure, params string[] sectionKeys)
            where T : class, new()
        {
            return serviceCollection.AddSingleton(serviceProvider =>
            {
                IConfiguration configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
                if (sectionKeys != null)
                {
                    foreach (var sectionKey in sectionKeys)
                    {
                        if (configuration == null)
                        {
                            throw new InvalidOperationException($"Configuration section key {sectionKey} not found.");
                        }

                        configuration = configuration.GetSection(sectionKey);
                    }
                }

                var t = new T();
                configuration.Bind(t);
                configure(serviceProvider, t);
                return t;
            });
        }

        private static IConfigurationBuilder AddJsonFiles(this IConfigurationBuilder builder, IServiceProvider serviceProvider)
        {
            foreach (var configurationPathProvider in serviceProvider.GetServices<IConfigurationPathProvider>())
            {
                string path = configurationPathProvider.Path;
                builder
                    .AddJsonFile(Path.Combine(path, "config.json"), serviceProvider)
                    .AddJsonFiles(Path.Combine(path, "conf.d"), serviceProvider);
            }

            return builder;
        }

        private static IConfigurationBuilder AddJsonFiles(this IConfigurationBuilder builder, string path, IServiceProvider serviceProvider)
        {
            if (Directory.Exists(path))
            {
                foreach (var filePath in Directory.GetFiles(path, "*.json").OrderBy(t => t))
                {
                    builder.AddJsonFile(filePath, serviceProvider);
                }
            }

            return builder;
        }

        private static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string path, IServiceProvider serviceProvider)
        {
            if (!File.Exists(path))
            {
                return builder;
            }

            Console.WriteLine($"Loading configuration from file: {path}");
            return builder.AddJsonFile(path, false);
        }
    }
}
