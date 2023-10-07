using Autofac;
using Frontend.Shared;

namespace Frontend.Desktop.Core
{
    public static class IoC
    {
        public static IContainer Container;

        public static void Build(ContainerBuilder builder = null)
        {
            if (builder == null)
            {
                builder = new ContainerBuilder();
            }

            builder.RegisterType<MessengerApiHelper>().SingleInstance();
            builder.RegisterType<MainViewModel>().SingleInstance();

            Container = builder.Build();
        }
    }
}
