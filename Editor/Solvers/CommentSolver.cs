using Dino.LocalizationKeyGenerator.Editor.Settings;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class CommentSolver {
        private readonly SolverImpl _solver = new SolverImpl();

        public CommentSolver() {
            UpdateSolverSettings();
            LocalizationKeyGeneratorSettings.Instance.Changed += UpdateSolverSettings;
        }

        private void UpdateSolverSettings() {
            _solver.DefaultStringFormat = LocalizationKeyGeneratorSettings.Instance.DefaultCommentStringFormat;
        }

        public bool TryCreateComment(PropertyContext context, string format, out string comment) {
            comment = null;
            _solver.ClearErrors();

            if (_solver.TryResolveFormat(context, format, out var resolvedFormat) == false) {
                return false;
            }

            _solver.CollectParameters(context);
            return _solver.TryResolveLine(context, resolvedFormat, out comment);
        }

        public void CheckForErrors(PropertyContext context, string format) => TryCreateComment(context, format, comment: out _);

        public string GetErrors() => _solver.GetErrors();
    }
}
