using GPOS.Core.Command;
using UnityEngine;

namespace GPOS.Core.Samples
{
    /// <summary>
    /// 커맨드 패턴(Undo/Redo) 예제.
    /// 씬에 배치하고 플레이하면 화면 좌상단 버튼으로 카운터를 조작할 수 있습니다.
    /// </summary>
    public class CommandExample : MonoBehaviour
    {
        private readonly CommandInvoker _invoker = new();
        private int _counter;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 220, 200));
            GUILayout.Label($"Counter: {_counter}");

            if (GUILayout.Button("+1 (Execute)"))
            {
                _invoker.Execute(new RelayCommand(
                    execute: () => _counter++,
                    undo: () => _counter--));
            }

            GUI.enabled = _invoker.CanUndo;
            if (GUILayout.Button($"Undo ({_invoker.UndoCount})"))
                _invoker.Undo();

            GUI.enabled = _invoker.CanRedo;
            if (GUILayout.Button($"Redo ({_invoker.RedoCount})"))
                _invoker.Redo();

            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
