using RoomTemplateEditor.Models;

namespace RoomTemplateEditor.Logic
{
    public class RuleReplacer
    {
        public static string[,] ApplyRules(MatrixTemplate matrixTemplate, List<Rule> rules)
        {
            var matrix = matrixTemplate.Matrix;
            foreach (var rule in rules.OrderByDescending(r => r.Priority))
            {
                matrix = ApplyRuleInternal(rule, matrix);
            }
            return matrix;
        }

        public static string[,] ApplyRule(MatrixTemplate matrixTemplate, Rule rule)
        {
            var matrix = (string[,])matrixTemplate.Matrix.Clone();
            return ApplyRuleInternal(rule, matrix);
        }

        private static string[,] ApplyRuleInternal(Rule rule, string[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            var patterns = GenerateTransformations(rule.Pattern, rule);
            var replacements = GenerateTransformations(rule.Replacement, rule);

            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                for (int row = -pattern.GetLength(0) + 1; row < rows; row++)
                {
                    for (int col = -pattern.GetLength(1) + 1; col < cols; col++)
                    {
                        if (MatchesPattern(matrix, pattern, row, col))
                        {
                            ApplyReplacement(matrix, replacements[i], row, col);
                        }
                    }
                }
            }
            return matrix;
        }

        private static bool MatchesPattern(string[,] matrix, string[,] pattern, int startRow, int startCol)
        {
            int matrixRows = matrix.GetLength(0);
            int matrixCols = matrix.GetLength(1);
            int patternRows = pattern.GetLength(0);
            int patternCols = pattern.GetLength(1);

            for (int r = 0; r < patternRows; r++)
            {
                for (int c = 0; c < patternCols; c++)
                {
                    var patternValue = pattern[r, c];
                    int matrixRow = startRow + r;
                    int matrixCol = startCol + c;

                    bool isOutOfBounds = matrixRow < 0 || matrixRow >= matrixRows || matrixCol < 0 || matrixCol >= matrixCols;

                    if (patternValue != null && patternValue != "any")
                    {
                        if (patternValue == "out")
                        {
                            if (!isOutOfBounds)
                            {
                                return false; // Expected out-of-bounds but found in-bounds
                            }
                        }
                        else
                        {
                            if (isOutOfBounds)
                            {
                                return false; // Expected in-bounds but found out-of-bounds
                            }
                            else if (matrix[matrixRow, matrixCol] != patternValue)
                            {
                                return false; // Values do not match
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void ApplyReplacement(string[,] matrix, string[,] replacement, int startRow, int startCol)
        {
            int matrixRows = matrix.GetLength(0);
            int matrixCols = matrix.GetLength(1);
            int replacementRows = replacement.GetLength(0);
            int replacementCols = replacement.GetLength(1);

            for (int r = 0; r < replacementRows; r++)
            {
                for (int c = 0; c < replacementCols; c++)
                {
                    var replacementValue = replacement[r, c];
                    int matrixRow = startRow + r;
                    int matrixCol = startCol + c;

                    bool isOutOfBounds = matrixRow < 0 || matrixRow >= matrixRows || matrixCol < 0 || matrixCol >= matrixCols;

                    if (replacementValue != null && replacementValue != "any" && !isOutOfBounds)
                    {
                        matrix[matrixRow, matrixCol] = replacementValue;
                    }
                }
            }
        }

        private static List<string[,]> GenerateTransformations(string[,] pattern, Rule rule)
        {
            var transformations = new List<string[,]>();
            var uniqueTransformations = new HashSet<string[,]>(new MatrixComparer());
            var queue = new Queue<string[,]>();
            queue.Enqueue(pattern);

            while (queue.Count > 0)
            {
                var currentPattern = queue.Dequeue();

                if (uniqueTransformations.Add(currentPattern))
                {
                    transformations.Add(currentPattern);

                    if (rule.AllowRotation)
                    {
                        queue.Enqueue(RotateMatrix(currentPattern));
                    }

                    if (rule.AllowHorizontalMirroring)
                    {
                        queue.Enqueue(MirrorHorizontally(currentPattern));
                    }

                    if (rule.AllowVerticalMirroring)
                    {
                        queue.Enqueue(MirrorVertically(currentPattern));
                    }
                }
            }

            return transformations;
        }

        private static string[,] RotateMatrix(string[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var rotated = new string[cols, rows];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    rotated[c, rows - r - 1] = matrix[r, c];
                }
            }

            return rotated;
        }

        private static string[,] MirrorHorizontally(string[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var mirrored = new string[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    mirrored[r, cols - c - 1] = matrix[r, c];
                }
            }

            return mirrored;
        }

        private static string[,] MirrorVertically(string[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var mirrored = new string[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    mirrored[rows - r - 1, c] = matrix[r, c];
                }
            }

            return mirrored;
        }

        private class MatrixComparer : IEqualityComparer<string[,]>
        {
            public bool Equals(string[,] x, string[,] y)
            {
                if (x.GetLength(0) != y.GetLength(0) || x.GetLength(1) != y.GetLength(1))
                {
                    return false;
                }

                for (int r = 0; r < x.GetLength(0); r++)
                {
                    for (int c = 0; c < x.GetLength(1); c++)
                    {
                        if (x[r, c] != y[r, c])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public int GetHashCode(string[,] obj)
            {
                int hash = 17;
                for (int r = 0; r < obj.GetLength(0); r++)
                {
                    for (int c = 0; c < obj.GetLength(1); c++)
                    {
                        hash = hash * 31 + (obj[r, c]?.GetHashCode() ?? 0);
                    }
                }
                return hash;
            }
        }
    }
}
