using System.Globalization;
using System;
using System.Linq;

namespace PR_1_3;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            Console.WriteLine("=== Меню ===");
            Console.WriteLine("1. Розв’язати задачу лінійного програмування (симплекс‑метод)");
            Console.WriteLine("2. Вихід");
            Console.Write("Оберіть пункт меню (1-2): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    RunSimplexMethod();
                    break;
                case "2":
                    Console.WriteLine("До побачення!");
                    return;
                default:
                    Console.WriteLine("Неправильний вибір. Спробуйте ще раз.");
                    break;
            }
        }
    }

    static void RunSimplexMethod()
    {
        string objectiveFunction = ReadObjectiveFunction();
        List<string> constraints = ReadConstraints();
        int variableCount = ReadVariableCount();

        object[,] simplexTable = BuildSimplexTable(objectiveFunction, constraints, variableCount);

        DisplayNormalizedConstraints(simplexTable, constraints.Count, variableCount);

        Console.WriteLine("\nПочаткова симплекс-таблиця:");
        DisplaySimplexTable(simplexTable);

        ComputeOptimalSolution(simplexTable);
    }

    // Зчитує функцію Z у вигляді рядка, наприклад: 2x1+3x2 -> max
    static string ReadObjectiveFunction()
    {
        Console.WriteLine("Введіть задачу лінійного програмування (наприклад  x1+2x2‑x3 -> max):");
        return Console.ReadLine();
    }

    // Зчитує обмеження у вигляді рядків, поки користувач не натисне Enter
    static List<string> ReadConstraints()
    {
        Console.WriteLine("Введіть обмеження (Порожній рядок - кінець):");
        List<string> constraints = new List<string>();
        string line;
        while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
        {
            constraints.Add(line);
        }
        return constraints;
    }

    // Зчитує кількість змінних у задачі
    static int ReadVariableCount()
    {
        Console.WriteLine("Введіть кількість змінних:");
        return int.Parse(Console.ReadLine());
    }

    // Функція для створення симплекс-таблиці
    static object[,] BuildSimplexTable(string inputZ, List<string> constraintExpressions, int variableCount)
    {
        double[] objectiveCoefficients = ParseObjectiveFunctionCoefficients(inputZ, variableCount);
        double[,] numericSimplexTable = new double[constraintExpressions.Count + 1, variableCount + 1];
        string[] basicVariableLabels = new string[constraintExpressions.Count + 1];
        basicVariableLabels[constraintExpressions.Count] = "Z=";

        for (int i = 0; i < constraintExpressions.Count; i++)
        {
            double[] constraintCoefficients = ParseConstraintCoefficients(constraintExpressions[i], variableCount);
            for (int j = 0; j < constraintCoefficients.Length; j++)
            {
                numericSimplexTable[i, j] = constraintCoefficients[j];
            }

            if (constraintExpressions[i].Contains(">="))
            {
                for (int j = 0; j < numericSimplexTable.GetLength(1); j++)
                {
                    numericSimplexTable[i, j] *= -1;
                }
            }

            basicVariableLabels[i] = "y" + (i + 1) + "=";
        }

        for (int j = 0; j < variableCount; j++)
        {
            numericSimplexTable[constraintExpressions.Count, j] = objectiveCoefficients[j];
        }

        FixFloatingPointErrorsInTable(numericSimplexTable);

        object[,] table = new object[numericSimplexTable.GetLength(0) + 1, numericSimplexTable.GetLength(1) + 1];
        table[0, 0] = "";
        for (int j = 0; j < variableCount; j++)
        {
            table[0, j + 1] = "-x" + (j + 1);
        }
        table[0, variableCount + 1] = "1";

        for (int i = 0; i < numericSimplexTable.GetLength(0); i++)
        {
            table[i + 1, 0] = basicVariableLabels[i];
            for (int j = 0; j < numericSimplexTable.GetLength(1); j++)
            {
                table[i + 1, j + 1] = numericSimplexTable[i, j];
            }
        }

        return table;
    }

    // Замінює майже нульові значення (наприклад, -0.00000001) на 0.00
    static void FixFloatingPointErrorsInTable(double[,] table)
    {
        for (int i = 0; i < table.GetLength(0); i++)
        {
            for (int j = 0; j < table.GetLength(1); j++)
            {
                if (Math.Abs(table[i, j]) < 1e-10)
                {
                    table[i, j] = 0.00;
                }
            }
        }
    }

    // Виводимо систему обмежень у нормалізованому вигляді
    static void DisplayNormalizedConstraints(object[,] table, int constraintsCount, int variableCount)
    {
        Console.WriteLine("x[j] >= 0, j=1," + variableCount);
        Console.WriteLine("Перепишемо систему обмежень:");

        for (int i = 0; i < constraintsCount; i++)
        {
            bool firstElement = true;
            for (int j = 0; j < variableCount; j++)
            {
                double value = Convert.ToDouble(table[i + 1, j + 1]);

                if (firstElement)
                {
                    Console.Write($"{(Math.Abs(value) < 1e-10 ? "+0,00" : $"{value:F2}")} * X[{j + 1}] ");
                    firstElement = false;
                }
                else
                {
                    if (Math.Abs(value) < 1e-10)
                    {
                        Console.Write("+0,00 * X[{0}] ", j + 1);
                    }
                    else
                    {
                        Console.Write((value >= 0 ? "+" : "") + $"{value:F2} * X[{j + 1}] ");
                    }
                }
            }

            double freeTerm = Convert.ToDouble(table[i + 1, variableCount + 1]);
            Console.WriteLine($"{(freeTerm >= 0 ? "+" : "")}{freeTerm:F2} >= 0");
        }
    }

    // Функція для пошуку оптимального розв’язку
    static void ComputeOptimalSolution(object[,] table)
    {
        Console.WriteLine("\n-Пошук опорного розв'язку-");
        while (CheckIfArtificialPhaseNeeded(table))
        {
            int pivotRow = SelectPivotRowForPhaseOne(table);
            int pivotColumn = SelectPivotColumnForPhaseOne(table, pivotRow);

            if (pivotRow == -1 || pivotColumn == -1)
            {
                Console.WriteLine("Неможливо знайти розв’язувальний елемент.");
                return;
            }

            Console.WriteLine($"\nРозв'язувальний стовпчик: {table[0, pivotColumn]}");
            Console.WriteLine($"Розв'язувальний рядочок: {table[pivotRow, 0].ToString().Replace("=", "").Trim()}");
            PerformJordanStepForFeasibility(table, pivotRow, pivotColumn);
            DisplaySimplexTable(table);
        }

        Console.WriteLine("Знайдено опорний розв'язок:");
        DisplayOptimalVariableValues(table);

        object[,] optimizationPhaseTable = new object[table.GetLength(0), table.GetLength(1)];
        CloneTable(table, optimizationPhaseTable);

        Console.WriteLine("\n-Пошук оптимального розв'язку-\n");
        DisplaySimplexTable(optimizationPhaseTable);

        bool isUnbounded = OptimizeSolutionByPivoting(optimizationPhaseTable);

        if (isUnbounded)
        {
            return;
        }

        Console.WriteLine("Знайдено оптимальний розв'язок:");
        DisplayOptimalVariableValues(optimizationPhaseTable);

        double lastElement = GetFinalObjectiveValue(optimizationPhaseTable);
        if (isMinimization)
        {
            Console.WriteLine($"Min (Z) = {-lastElement}");
        }
        else
        {
            Console.WriteLine($"Max (Z) = {lastElement}");
        }

    }



    static bool isMinimization = false;  // глобальна змінна

    static double[] ParseObjectiveFunctionCoefficients(string input, int variableCount)
    {
        isMinimization = input.Trim().ToLower().Contains("min");
        input = input.Split("->")[0].Replace(" ", "");

        double[] coefficients = new double[variableCount];

        for (int i = 1; i <= variableCount; i++)
        {
            var parts = input.Split(new string[] { $"x{i}" }, StringSplitOptions.None);
            string term = parts.Length > 1 ? parts[0] : "0";
            input = parts.Length > 1 ? parts[1] : input;
            coefficients[i - 1] = string.IsNullOrEmpty(term) || term == "+" ? 1 : term == "-" ? -1 : double.Parse(term, CultureInfo.InvariantCulture);
        }

        // Для задачі мінімізації — інвертуємо
        if (isMinimization)
            coefficients = coefficients.Select(x => -x).ToArray();

        // У симплекс-методі все одно працюємо з -Z
        return coefficients.Select(x => -x).ToArray();
    }



    static double[] ParseConstraintCoefficients(string constraint, int variableCount)
    {
        double[] coefficients = new double[variableCount + 1];
        constraint = constraint.Replace(" ", "").Replace("<=", " ").Replace(">=", " ");
        string[] parts = constraint.Split(' ');

        for (int i = 1; i <= variableCount; i++)
        {
            var subParts = parts[0].Split(new string[] { $"x{i}" }, StringSplitOptions.None);
            string term = subParts.Length > 1 ? subParts[0] : "0";
            parts[0] = subParts.Length > 1 ? subParts[1] : parts[0];
            coefficients[i - 1] = string.IsNullOrEmpty(term) || term == "+" ? 1 : term == "-" ? -1 : double.Parse(term);
        }

        coefficients[variableCount] = double.Parse(parts[1]);
        if (constraint.Contains(">="))
        {
            coefficients = coefficients.Select(x => -x).ToArray();
        }
        return coefficients;
    }



    static double GetFinalObjectiveValue(object[,] table)
    {
        int lastRow = table.GetLength(0) - 1;
        int lastCol = table.GetLength(1) - 1;

        if (table[lastRow, lastCol] is double)
        {
            return (double)table[lastRow, lastCol];
        }
        return Convert.ToDouble(table[lastRow, lastCol]);
    }


    static bool OptimizeSolutionByPivoting(object[,] table)
    {
        while (true)
        {
            int pivotColumn = FindPivotColumnInObjectiveRow(table);

            if (pivotColumn == -1)
            {
                Console.WriteLine("У рядку Z немає від’ємних елементів.");
                return false;
            }

            Console.WriteLine($"Розв'язувальний стовпчик: {table[0, pivotColumn]}");

            int rowCount = table.GetLength(0) - 1;
            double[] thetaRatios = new double[rowCount];
            for (int i = 1; i < rowCount; i++)
            {
                double denominator = Convert.ToDouble(table[i, pivotColumn]);
                if (denominator <= 0)
                {
                    thetaRatios[i] = double.MaxValue;
                }
                else
                {
                    double numerator = Convert.ToDouble(table[i, table.GetLength(1) - 1]);
                    thetaRatios[i] = numerator / denominator;
                }
            }

            double minRatio = double.MaxValue;
            int pivotRow = -1;
            for (int i = 1; i < rowCount; i++)
            {
                if (thetaRatios[i] >= 0 && thetaRatios[i] < minRatio)
                {
                    minRatio = thetaRatios[i];
                    pivotRow = i;
                }
            }

            if (pivotRow == -1)
            {
                if (isMinimization)
                    Console.WriteLine("Не знайдено розв'язувальний рядок. Цільова функція Z не обмежена знизу.");
                else
                    Console.WriteLine("Не знайдено розв'язувальний рядок. Цільова функція Z не обмежена зверху.");
                return true;
            }

            Console.WriteLine($"Розв'язувальний рядочок: {table[pivotRow, 0].ToString().Replace("=", "").Trim()}");

            PerformJordanStepForOptimization(table, pivotRow, pivotColumn);
            DisplaySimplexTable(table);
        }
    }

    static int FindPivotColumnInObjectiveRow(object[,] table)
    {
        int pivotColumn = -1;
        int objectiveRowIndex = table.GetLength(0) - 1;
        int lastColumn = table.GetLength(1) - 2;

        for (int j = 1; j <= lastColumn; j++)
        {
            if (Convert.ToDouble(table[objectiveRowIndex, j]) < 0)
            {
                pivotColumn = j;
                break;
            }
        }

        return pivotColumn;
    }

    // Оновлюємо назви базисних змінних після обміну (pivot)
    static void PerformJordanStepForOptimization(object[,] table, int pivotRow, int pivotColumn)
    {
        int rowCount = table.GetLength(0);
        int colCount = table.GetLength(1);
        double pivotValue = Convert.ToDouble(table[pivotRow, pivotColumn]);
        Console.WriteLine($"Розв'язувальний елемент:  {Convert.ToDouble(pivotValue):F2}");

        table[pivotRow, pivotColumn] = 1.0;

        for (int i = 1; i < rowCount; i++)
        {
            for (int j = 1; j < colCount; j++)
            {
                if (i != pivotRow && j != pivotColumn)
                {
                    table[i, j] = (Convert.ToDouble(table[i, j]) * pivotValue - Convert.ToDouble(table[i, pivotColumn]) * Convert.ToDouble(table[pivotRow, j])) / pivotValue;
                }
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = -Convert.ToDouble(table[i, pivotColumn]);
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = Convert.ToDouble(table[i, pivotColumn]) / pivotValue;
            }
        }

        for (int j = 1; j < colCount; j++)
        {
            table[pivotRow, j] = Convert.ToDouble(table[pivotRow, j]) / pivotValue;
        }

        double[] columnBeforePivoting = new double[rowCount];
        for (int i = 1; i < rowCount; i++)
        {
            columnBeforePivoting[i] = Convert.ToDouble(table[i, pivotColumn]);
        }

        UpdateVariableLabelsAfterPivot(table, pivotRow, pivotColumn);
    }



    static void DisplayOptimalVariableValues(object[,] table)
    {
        int variableCount = table.GetLength(1) - 2;
        double[] solution = new double[variableCount];

        for (int i = 1; i < table.GetLength(0) - 1; i++)
        {
            string rowName = table[i, 0].ToString();
            if (rowName.StartsWith("x") && rowName.Contains("="))
            {
                rowName = rowName.Replace("=", "").Trim();

                int index = int.Parse(rowName.Substring(1)) - 1;
                solution[index] = Convert.ToDouble(table[i, table.GetLength(1) - 1]);
            }
        }

        Console.Write("X(");
        for (int i = 0; i < solution.Length; i++)
        {
            Console.Write(solution[i]);
            if (i < solution.Length - 1) Console.Write("; ");
        }
        Console.WriteLine(")");
    }
    static void CloneTable(object[,] source, object[,] destination)
    {
        for (int i = 0; i < source.GetLength(0); i++)
        {
            for (int j = 0; j < source.GetLength(1); j++)
            {
                destination[i, j] = source[i, j];
            }
        }
    }
    static bool CheckIfArtificialPhaseNeeded(object[,] table)
    {
        for (int i = 1; i < table.GetLength(0) - 1; i++)
        {
            if (Convert.ToDouble(table[i, table.GetLength(1) - 1]) < 0)
                return true;
        }
        return false;
    }
    static int SelectPivotColumnForPhaseOne(object[,] table, int pivotRow)
    {
        for (int j = 1; j < table.GetLength(1) - 1; j++)
        {
            if (Convert.ToDouble(table[pivotRow, j]) < 0)
                return j;
        }
        return -1;
    }
    static int SelectPivotRowForPhaseOne(object[,] table)
    {
        int rowCount = table.GetLength(0) - 1;
        int pivotRow = -1;
        for (int i = 1; i < rowCount; i++)
        {
            if (Convert.ToDouble(table[i, table.GetLength(1) - 1]) < 0)
            {
                pivotRow = i;
                break;
            }
        }
        return pivotRow;
    }



    static void PerformJordanStepForFeasibility(object[,] table, int pivotRow, int pivotColumn)
    {
        int rowCount = table.GetLength(0);
        int colCount = table.GetLength(1);
        double pivotElement = Convert.ToDouble(table[pivotRow, pivotColumn]);
        Console.WriteLine($"Розв'язувальний елемент: {Convert.ToDouble(pivotElement):F2}");

        table[pivotRow, pivotColumn] = 1.0;

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = -Convert.ToDouble(table[i, pivotColumn]);
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = Convert.ToDouble(table[i, pivotColumn]) / pivotElement;
            }
        }

        for (int j = 1; j < colCount; j++)
        {
            table[pivotRow, j] = Convert.ToDouble(table[pivotRow, j]) / pivotElement;
        }

        double[] pivotColumnValues = new double[rowCount];
        for (int i = 1; i < rowCount; i++)
        {
            pivotColumnValues[i] = Convert.ToDouble(table[i, pivotColumn]);
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i == pivotRow) continue;
            double factor = pivotColumnValues[i];
            for (int j = 1; j < colCount; j++)
            {
                if (j == pivotColumn)
                    table[i, j] = pivotColumnValues[i];
                else
                    table[i, j] = Convert.ToDouble(table[i, j]) - factor * Convert.ToDouble(table[pivotRow, j]);
            }
        }

        UpdateVariableLabelsAfterPivot(table, pivotRow, pivotColumn);
    }
    static void DisplaySimplexTable(object[,] table)
    {
        int rows = table.GetLength(0);
        int cols = table.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (table[i, j] is double)
                {
                    double value = Convert.ToDouble(table[i, j]);
                    string formattedValue = value == -0.00 ? "0.00" : value.ToString("F2");
                    Console.Write($"{formattedValue}\t");
                }
                else
                {
                    Console.Write($"{table[i, j]}\t");
                }
            }
            Console.WriteLine();
        }
    }

    // Оновлюємо назви базисних змінних після обміну (pivot)
    static void UpdateVariableLabelsAfterPivot(object[,] table, int pivotRow, int pivotColumn)
    {
        string columnName = table[0, pivotColumn].ToString();
        string rowName = table[pivotRow, 0].ToString();

        string cleanedColumnName = columnName.StartsWith("-") ? columnName.Substring(1) : columnName;
        string cleanedRowName = rowName.StartsWith("-") ? rowName.Substring(1) : rowName;

        bool rowHasEqual = cleanedRowName.Contains("=");

        if (rowHasEqual)
        {
            cleanedRowName = cleanedRowName.Replace("=", "").Trim();
        }

        table[0, pivotColumn] = "-" + cleanedRowName;
        table[pivotRow, 0] = cleanedColumnName + (rowHasEqual ? "=" : "");
    }


}

