using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PrintMate.Terminal.Views.Configure.ConfigureParametersViews;

namespace PrintMate.Terminal.AppConfiguration
{
    public class Permission
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public Permission(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
    public class Permissions
    {
        // Control
        public static Permission ManualAxesControl = new Permission("ManualAxesControl", "Раздел Управление/Оси");
        public static Permission ManualControlSystems = new Permission("ManualControlSystems", "Раздел Управление/Системы");

        // Process
        public static Permission ConfigureProcessSystemView = new Permission("ConfigureProcessSystemView", "Раздел Конфигурация/Процесс/Система");
        public static Permission ConfigureProcessGas = new Permission("ConfigureProcessGas", "Раздел Конфигурация/Процесс/Газ");
        public static Permission ConfigureProcessLaser = new Permission("ConfigureProcessLaser", "Раздел Конфигурация/Процесс/Лазер");
        public static Permission ConfigureProcessPowder = new Permission("ConfigureProcessPowder", "Раздел Конфигурация/Процесс/Порошок");
        public static Permission ConfigureProcessService = new Permission("ConfigureProcessService", "Раздел Конфигурация/Процесс/Сервис");

        // Parameters
        public static Permission ConfigureParametersRoles = new Permission("ConfigureParametersRoles", "Раздел Конфигурация/Параметры/Роли");
        public static Permission ConfigureParametersUsers = new Permission("ConfigureParametersUsers", "Раздел Конфигурация/Параметры/Плоьзователи");
        public static Permission ConfigureParametersPlc = new Permission("ConfigureParametersPlc", "Раздел Конфигурация/Параметры/ПЛК");
        public static Permission ConfigureParametersScanator = new Permission("ConfigureParametersScanator", "Раздел Конфигурация/Параметры/Сканаторы");
        public static Permission ConfigureParametersLasers = new Permission("ConfigureParametersLasers", "Раздел Конфигурация/Параметры/Лазеры");
        public static Permission ConfigureParametersAutomaticSettings = new Permission("ConfigureParametersAutomaticSettings", "Раздел Конфигурация/Параметры/Параметры");
        public static Permission ConfigureParametersStorage = new Permission("ConfigureParametersStorage", "Раздел Конфигурация/Параметры/Хранение");
        public static Permission ConfigureParametersCamera = new Permission("ConfigureParametersCamera", "Раздел Конфигурация/Параметры/Камера");
        public static Permission ConfigureParametersServicesStates = new Permission("ConfigureParametersServicesStates", "Раздел Конфигурация/Параметры/Сервисы");
        public static Permission ConfigureParametersAdditionalSoftware = new Permission("ConfigureParametersAdditionalSoftware", "Раздел Конфигурация/Параметры/Стороннее ПО");
        public static Permission ConfigureParametersComputerVision = new Permission("ConfigureParametersComputerVision",
            "Раздел Конфигурация/Параметры/Машинное зрение");


        // Print actions
        public static Permission ProjectsView = new Permission("ProjectsView", "Раздел Проекты");
        public static Permission PrintPageView = new Permission("PrintPageView", "Раздел Печать");

        private static List<Permission> _perms;
        public static List<Permission> Perms
        {
            get
            {
                if (_perms == null) _perms = Parse();
                return _perms;
            }
        }

        private static List<Permission> Parse()
        {
            var list = new List<Permission>();
            foreach (var field in typeof(Permissions).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(Permission))
                {
                    var value = (Permission)field.GetValue(null);
                    list.Add(value);
                }
            }
            return list;
        }
    }
}
