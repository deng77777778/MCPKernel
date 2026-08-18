using MCP.Kernel.ScanAssembly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MCP.Kernel.Bootstrap
{
    public sealed class ScanAssemblyBootstrap : IBootstrap
    {
        public int Order => (int)BootstrapEnum.ScanAssembly;

        public void Initialize()
        {
            var scanList = MCP.Kernel.ScanTypeGenerated.CreateAllInstances();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !a.FullName.StartsWith("System")
                             && !a.FullName.StartsWith("Microsoft")
                             && !a.FullName.StartsWith("UnityEngine")
                             && !a.FullName.StartsWith("Unity"));

            foreach (var assembly in assemblies)
            {
                try
                {
                    ScanAssembly(assembly, scanList);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Assembly Scanner] Failed to scan assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 扫描单个程序集
        /// </summary>
        public void ScanAssembly(Assembly assembly, List<IScanType> list)
        {
            //Debug.Log(assembly.GetName().Name);

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                foreach (var scan in list)
                {
                    if (scan.AllowScan(type))
                        scan.ScanType(type);
                }
            }
        }
    }
}
