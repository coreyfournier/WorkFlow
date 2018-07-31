using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Unity.Lifetime;
using Workflow.Orchestration.Queues;

namespace Workflow.Orchestration
{
    public class DependencyContainer
    {
        static UnityContainer _container = new UnityContainer();

        private DependencyContainer()
        {
            
        }

        /// <summary>
        /// Register a custom dependency for IObservedChangeQueue or ISubscriberQueue
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <returns></returns>
        public static IUnityContainer RegisterType<TFrom, TTo>() where TTo : TFrom
        {
            return _container.RegisterType<TFrom, TTo>();
        }

        /// <summary>
        /// Register a custom dependency for IObservedChangeQueue or ISubscriberQueue
        /// </summary>
        /// <typeparam name="TFrom">Instance of type to register</typeparam>
        /// <returns></returns>
        public static IUnityContainer Register<TFrom>(TFrom instance) where TFrom : class
        {
            return _container.RegisterInstance<TFrom>(instance);
        }

        /// <summary>
        /// Registers the concrete implementations for WCF queues (IObservedChangeQueue, ISubscriberQueue)
        /// </summary>
        public static void RegisterDefaults()
        {
            _container.RegisterType<IObservedChangeQueue, ObservedChangeQueue>();
            _container.RegisterType<ISubscriberQueue, SubscriberQueue>();
        }

        /// <summary>
        /// Resolves the queues
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}
