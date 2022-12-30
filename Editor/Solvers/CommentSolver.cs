using Sirenix.OdinInspector.Editor;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class CommentSolver {
        private readonly SolverImpl _solver = new SolverImpl();
        
        public bool TryCreateComment(InspectorProperty property, string format, out string comment) {
            comment = null;
            _solver.ClearErrors();

            if (_solver.TryResolveFormat(property, format, out var resolvedFormat) == false) {
                return false;
            }

            _solver.CollectParameters(property);
            return _solver.TryFormatLine(property, resolvedFormat, out comment);
        }
        
        public void CheckForErrors(InspectorProperty property, string format) => TryCreateComment(property, format, comment: out _);

        public string GetErrors() => _solver.GetErrors();
    }
}