﻿using System;
using System.Linq;
using Prism.Ioc;
using Prism.Navigation;
using Unity;
using Unity.Resolution;
using Windows.UI.Xaml.Controls;

namespace Prism
{
    public class UnityContainerExtension : IContainerExtension<IUnityContainer>
    {
        public IUnityContainer Instance { get; }

        public bool SupportsModules => true;

        public UnityContainerExtension(IUnityContainer container) => Instance = container;

        public void FinalizeExtension() { }

        public void RegisterInstance(Type type, object instance)
        {
            Instance.RegisterInstance(type, instance);
        }

        public void RegisterSingleton(Type from, Type to)
        {
            Instance.RegisterSingleton(from, to);
        }

        public void Register(Type from, Type to)
        {
            Instance.RegisterType(from, to);
        }

        public void Register(Type from, Type to, string name)
        {
            Instance.RegisterType(from, to, name);
        }

        public object Resolve(Type type)
        {
            return Instance.Resolve(type);
        }

        public object Resolve(Type type, string name)
        {
            return Instance.Resolve(type, name);
        }

        public object ResolveViewModelForView(object view, Type viewModelType)
        {
            ParameterOverrides overrides = null;
            if (view is Page page)
            {
                var service = NavigationService.Instances[page.Frame];
                overrides = new ParameterOverrides
                {
                    { PrismApplicationBase.NavigationServiceParameterName, service }
                };
            }
            return Instance.Resolve(viewModelType, overrides);
        }
    }
}
