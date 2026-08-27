using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BigAmbitions.DayNightCycle;
using Helpers;
using Timemachine;
using UI;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace VoogleRoute
{
    /// <summary>
    /// Centralizes Big Ambitions 0.11/1.0 API differences. Calls whose
    /// signatures changed must stay reflection-only so Mono never binds the
    /// unavailable overload before the capability check runs.
    /// </summary>
    internal static class BigAmbitionsCompatibility
    {
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                              BindingFlags.Instance | BindingFlags.Static;

        private static readonly PropertyInfo CityMapTaxiModeProperty =
            typeof(CityMap).GetProperty("IsTaxiMode", AllFlags);
        private static readonly FieldInfo CityMapTaxiModeField =
            typeof(CityMap).GetField("isTaxiMode", AllFlags);
        private static readonly PropertyInfo TaxiTravelingProperty =
            typeof(TaxiSystem).GetProperty("IsTraveling", AllFlags);
        private static readonly FieldInfo LegacyTaxiDestinationField =
            typeof(TaxiSystem).GetField("cityBuildingController", AllFlags);
        private static readonly MethodInfo FadeMethod = FindBestMethod(
            typeof(UiFader),
            "Fade",
            isStatic: true,
            firstParameterType: typeof(float));
        private static readonly MethodInfo StartTimeMachineMethod = FindStartTimeMachineMethod();
        private static readonly MethodInfo CurrentEntranceMethod = FindStaticAddressMethod(
            typeof(BuildingHelper),
            "GetAddressEntranceTransform");
        private static readonly MethodInfo LegacyEntranceMethod = FindStaticAddressMethod(
            typeof(DeliveryJobHelper),
            "GetAddressEntranceTransform");

        internal static bool IsTaxiMapMode(CityMap cityMap)
        {
            if (cityMap == null)
                return false;

            try
            {
                if (CityMapTaxiModeProperty?.GetValue(cityMap) is bool propertyValue)
                    return propertyValue;

                return CityMapTaxiModeField?.GetValue(cityMap) is bool fieldValue && fieldValue;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsTaxiTravelActive()
        {
            try
            {
                TaxiSystem instance = null;
                if (InstanceBehavior<TaxiSystem>.IsInitialized)
                    instance = InstanceBehavior<TaxiSystem>.Instance;

                if (TaxiTravelingProperty != null)
                {
                    var target = TaxiTravelingProperty.GetMethod?.IsStatic == true ? null : instance;
                    return TaxiTravelingProperty.GetValue(target) is bool traveling && traveling;
                }

                return instance != null && LegacyTaxiDestinationField?.GetValue(instance) != null;
            }
            catch
            {
                return false;
            }
        }

        internal static IEnumerator Fade()
        {
            try
            {
                return FadeMethod?.Invoke(null, BuildDefaultArguments(FadeMethod)) as IEnumerator ??
                       EmptyCoroutine();
            }
            catch
            {
                return EmptyCoroutine();
            }
        }

        internal static bool TryStartTimeMachine(
            TimeMachine timeMachine,
            Timestamp timestamp,
            bool disableCancel)
        {
            if (timeMachine == null || StartTimeMachineMethod == null)
                return false;

            try
            {
                var parameters = StartTimeMachineMethod.GetParameters();
                var arguments = new object[parameters.Length];
                arguments[0] = timestamp;

                for (var index = 1; index < parameters.Length; index++)
                {
                    var parameter = parameters[index];
                    arguments[index] = string.Equals(
                        parameter.Name,
                        "disableCancel",
                        StringComparison.OrdinalIgnoreCase)
                        ? disableCancel
                        : GetDefaultValue(parameter);
                }

                StartTimeMachineMethod.Invoke(timeMachine, arguments);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("Compatible TimeMachine start failed", Unwrap(ex));
                return false;
            }
        }

        internal static Transform GetAddressEntranceTransform(Address address)
        {
            if (address == null)
                return null;

            try
            {
                var method = CurrentEntranceMethod ?? LegacyEntranceMethod;
                return method?.Invoke(null, new object[] { address }) as Transform;
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo FindStartTimeMachineMethod()
        {
            var methods = typeof(TimeMachine).GetMethods(AllFlags)
                .Where(method =>
                    method.Name == "StartTimeMachine" &&
                    !method.IsStatic &&
                    method.GetParameters().Length > 0 &&
                    method.GetParameters()[0].ParameterType == typeof(Timestamp))
                .OrderByDescending(method => method.GetParameters().Any(parameter =>
                    string.Equals(parameter.Name, "disableCancel", StringComparison.OrdinalIgnoreCase)))
                .ThenByDescending(method => method.GetParameters().Length);

            return methods.FirstOrDefault();
        }

        private static MethodInfo FindStaticAddressMethod(Type type, string name) =>
            type.GetMethod(
                name,
                AllFlags,
                null,
                new[] { typeof(Address) },
                null);

        private static MethodInfo FindBestMethod(
            Type type,
            string name,
            bool isStatic,
            Type firstParameterType)
        {
            return type.GetMethods(AllFlags)
                .Where(method => method.Name == name && method.IsStatic == isStatic)
                .Where(method =>
                {
                    var parameters = method.GetParameters();
                    return parameters.Length > 0 && parameters[0].ParameterType == firstParameterType;
                })
                .OrderByDescending(method => method.GetParameters().Length)
                .FirstOrDefault();
        }

        private static object[] BuildDefaultArguments(MethodInfo method)
        {
            if (method == null)
                return Array.Empty<object>();

            var parameters = method.GetParameters();
            var arguments = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
                arguments[index] = GetDefaultValue(parameters[index]);
            return arguments;
        }

        private static object GetDefaultValue(ParameterInfo parameter)
        {
            var declaredDefault = parameter.DefaultValue;
            if (declaredDefault != null &&
                declaredDefault != DBNull.Value &&
                declaredDefault != Type.Missing)
                return declaredDefault;

            return parameter.ParameterType.IsValueType
                ? Activator.CreateInstance(parameter.ParameterType)
                : null;
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }

        private static Exception Unwrap(Exception exception) =>
            exception is TargetInvocationException { InnerException: not null } invocation
                ? invocation.InnerException
                : exception;
    }
}
