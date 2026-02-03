using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace EditorUtilities
{
    [CreateAssetMenu(fileName = "KillProcessScriptable", menuName = "Tools/Kill Process Scriptable")]
    public class KillProcessScriptable : ScriptableObject
    {
        [Header("Текущий (главный) процесс Unity")]
        [SerializeField, Disabled] private int unityProcessID = -1;
        [SerializeField, Disabled] private string unityProcessName = string.Empty;

        [Header("Шаблон поиска")]
        public string moduleNamePart = "CustomHardware.dll";

        [SerializeField, Disabled] private List<int> processesToKill = new List<int>();

        private void Awake()
        {
            var mainProcess = Process.GetCurrentProcess();
            unityProcessID = mainProcess.Id;
            unityProcessName = mainProcess.ProcessName;
            Debug.Log($"Unity process \nID: {unityProcessID}\nName: {unityProcessName}");
        }

        public void MarkProcessesToKillByNamePartOfConnectedModule()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MAIN Unity process: \nID = {unityProcessID}\nName = {unityProcessName}");
            sb.AppendLine("- \t\t - - - \t\t -");

            Process[] allProcesses = Process.GetProcesses();

            var matchedProcesses = allProcesses
                .Where(p => p.Id != unityProcessID && p.ProcessName.ToLower().Equals(unityProcessName.ToLower()))
                .ToList();

            if (matchedProcesses.Count == 0)
            {
                sb.AppendLine($"Процессов с именем, совпадающим с '{moduleNamePart}', не найдено.");
                Debug.LogWarning(sb);
                return;
            }

            foreach (var process in matchedProcesses)
            {
                var needToKill = false;
                ProcessModuleCollection unityModules = process.Modules;
                sb.AppendLine($"{process.ProcessName} (PID: {process.Id}) Process \nContains Modules: ...");
                foreach (ProcessModule module in unityModules)
                {
                    if (module.ModuleName.ToLower().Contains(moduleNamePart.ToLower()) || module.FileName.ToLower().Contains(moduleNamePart.ToLower()))
                    {
                        sb.AppendLine($". . . . .");
                        sb.AppendLine($"|\tModule Name:\n|\t{module.ModuleName}");
                        sb.AppendLine($"|\tModule FileName:\n|\t {module.FileName}");
                        sb.AppendLine($"- - - - -");
                        needToKill = true;
                    }
                }
                if (needToKill)
                {
                    sb.AppendLine($"Процесс '{process.ProcessName}'[PID: {process.Id}] помечен на завершение.");
                    sb.AppendLine($"^ ^ ^ ^ ^");
                    processesToKill.Add(process.Id);
                }
            }
            Debug.Log(sb);
        }

        public void KillMarkedProcesses()
        {
            if (processesToKill.Count > 0)
            {
                foreach (var processId in processesToKill)
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                }
                processesToKill.Clear();
                Debug.Log($"Все отмеченные процессы на завершение - завершены.");
            }
            else
            {
                Debug.LogWarning("Нет процессов помеченных на заверщение");
            }
        }
    }
}
