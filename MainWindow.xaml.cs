using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SadCalculator {
    public partial class MainWindow : Window {
        private readonly CalculatorEngine calculator;
        private readonly CommandManager commandManager;
        private bool isScientificMode = false;

        public MainWindow() {
            InitializeComponent();
            calculator = new CalculatorEngine();
            commandManager = new CommandManager();
            this.KeyDown += MainWindow_KeyDown;
            UpdateDisplay();
        }

        private void UpdateDisplay() {
            ResultDisplay.Text = calculator.DisplayValue;
            CalculationDisplay.Text = calculator.CalculationExpression;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.NumPad0:
                case Key.D0:
                    ProcessNumberInput("0");
                    break;
                case Key.NumPad1:
                case Key.D1:
                    ProcessNumberInput("1");
                    break;
                case Key.NumPad2:
                case Key.D2:
                    ProcessNumberInput("2");
                    break;
                case Key.NumPad3:
                case Key.D3:
                    ProcessNumberInput("3");
                    break;
                case Key.NumPad4:
                case Key.D4:
                    ProcessNumberInput("4");
                    break;
                case Key.NumPad5:
                case Key.D5:
                    ProcessNumberInput("5");
                    break;
                case Key.NumPad6:
                case Key.D6:
                    ProcessNumberInput("6");
                    break;
                case Key.NumPad7:
                case Key.D7:
                    ProcessNumberInput("7");
                    break;
                case Key.NumPad8:
                case Key.D8:
                    ProcessNumberInput("8");
                    break;
                case Key.NumPad9:
                case Key.D9:
                    ProcessNumberInput("9");
                    break;
                case Key.Add:
                    ProcessOperator("+");
                    break;
                case Key.Subtract:
                    ProcessOperator("-");
                    break;
                case Key.Multiply:
                    ProcessOperator("*");
                    break;
                case Key.Divide:
                    ProcessOperator("/");
                    break;
                case Key.Enter:
                    CalculateResult();
                    break;
                case Key.Escape:
                    Clear_Click(null, null);
                    break;
                case Key.Back:
                    Backspace_Click(null, null);
                    break;
                case Key.Decimal:
                case Key.OemPeriod:
                case Key.OemComma:
                    Decimal_Click(null, null);
                    break;
                case Key.Z:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Undo_Click(null, null);
                    else if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                        Redo_Click(null, null);
                    break;
            }
        }

        private void Number_Click(object sender, RoutedEventArgs e) {
            string number = ((Button)sender).Content.ToString();
            ProcessNumberInput(number);
        }

        private void Decimal_Click(object sender, RoutedEventArgs e) {
            if (calculator.IsInErrorState)
                calculator.ResetErrorState();

            var command = new DecimalCommand(calculator);
            commandManager.ExecuteCommand(command);
            UpdateDisplay();
        }

        private void Operator_Click(object sender, RoutedEventArgs e) {
            string op = ((Button)sender).Content.ToString();
            ProcessOperator(op);
        }

        private void Equals_Click(object sender, RoutedEventArgs e) {
            CalculateResult();
        }

        private void Clear_Click(object sender, RoutedEventArgs e) {
            var command = new ClearCommand(calculator);
            commandManager.ExecuteCommand(command);
            UpdateDisplay();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e) {
            if (calculator.IsInErrorState) {
                calculator.ResetErrorState();
                UpdateDisplay();
                return;
            }

            if (!calculator.IsNewOperation && calculator.DisplayValue.Length > 0) {
                var command = new BackspaceCommand(calculator);
                commandManager.ExecuteCommand(command);
                UpdateDisplay();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e) {
            commandManager.Undo();
            UpdateDisplay();
        }

        private void Redo_Click(object sender, RoutedEventArgs e) {
            commandManager.Redo();
            UpdateDisplay();
        }

        private void ToggleScientific_Click(object sender, RoutedEventArgs e) {
            isScientificMode = !isScientificMode;
            
            if (isScientificMode) {
                ExtraColumn.Width = new GridLength(1, GridUnitType.Star);
                this.Width += 80;
                MenuButton.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
                
                foreach (UIElement element in ButtonsGrid.Children) {
                    if (Grid.GetColumn(element) == 4)
                        element.Visibility = Visibility.Visible;
                }
            } else {
                ExtraColumn.Width = new GridLength(0);
                this.Width -= 80;
                MenuButton.Visibility = Visibility.Visible;
                CollapseButton.Visibility = Visibility.Collapsed;
                
                foreach (UIElement element in ButtonsGrid.Children) {
                    if (Grid.GetColumn(element) == 4)
                        element.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Scientific_Click(object sender, RoutedEventArgs e) {
            if (calculator.IsInErrorState) {
                calculator.ResetErrorState();
                UpdateDisplay();
            }

            string operation = ((Button)sender).Content.ToString();
            try {
                var command = new ScientificCommand(calculator, operation);
                commandManager.ExecuteCommand(command);
                UpdateDisplay();
            } catch {
                calculator.SetErrorState();
                UpdateDisplay();
            }
        }

        private void ProcessNumberInput(string number) {
            if (calculator.IsInErrorState)
                calculator.ResetErrorState();

            var command = new InputCommand(calculator, number);
            commandManager.ExecuteCommand(command);
            UpdateDisplay();
        }

        private void ProcessOperator(string op) {
            if (calculator.IsInErrorState)
                calculator.ResetErrorState();

            try {
                // if we have an incomplete calculation, complete it
                if (!string.IsNullOrEmpty(calculator.CalculationExpression) && !calculator.IsNewOperation)
                    CalculateResult();
                
                var command = new OperatorCommand(calculator, op);
                commandManager.ExecuteCommand(command);
                UpdateDisplay();
            } catch {
                calculator.SetErrorState();
                UpdateDisplay();
            }
        }

        private void CalculateResult() {
            if (calculator.IsInErrorState || string.IsNullOrEmpty(calculator.CalculationExpression) || calculator.IsNewOperation)
                return;

            try {
                var command = new CalculateCommand(calculator);
                commandManager.ExecuteCommand(command);
                UpdateDisplay();
            } catch {
                calculator.SetErrorState();
                UpdateDisplay();
            }
        }
    }

    public class CalculatorEngine
    {
        public string DisplayValue { get; private set; } = "0";
        public string CalculationExpression { get; private set; } = "";
        public bool IsNewOperation { get; private set; } = true;
        public bool IsInErrorState { get; private set; } = false;

        public void SetDisplayValue(string value) => DisplayValue = value;

        public void SetCalculationExpression(string expression) => CalculationExpression = expression;

        public void SetNewOperation(bool value) => IsNewOperation = value;

        public void ResetErrorState() {
            if (IsInErrorState) {
                DisplayValue = "0";
                CalculationExpression = "";
                IsNewOperation = true;
                IsInErrorState = false;
            }
        }

        public void SetErrorState() {
            DisplayValue = "Error";
            CalculationExpression = "";
            IsNewOperation = true;
            IsInErrorState = true;
        }

        public void AddDecimalPoint() {
            if (IsNewOperation) {
                DisplayValue = "0.";
                IsNewOperation = false;
            } else if (!DisplayValue.Contains(".")) {
                DisplayValue += ".";
            }
        }

        public void Backspace() {
            if (DisplayValue.Length == 1 || (DisplayValue.Length == 2 && DisplayValue[0] == '-'))
            {
                DisplayValue = "0";
                IsNewOperation = true;
            } else {
                DisplayValue = DisplayValue[..^1];
            }
        }

        public void Clear() {
            DisplayValue = "0";
            CalculationExpression = "";
            IsNewOperation = true;
        }

        public void ProcessNumber(string number) {
            if (IsNewOperation) {
                DisplayValue = number;
                IsNewOperation = false;
            }
            else {
                if (DisplayValue == "0" && number != ".")
                    DisplayValue = number;
                else
                    DisplayValue += number;
            }
        }

        public void ApplyOperator(string op) {
            CalculationExpression = $"{DisplayValue} {op}";
            IsNewOperation = true;
        }

        public void PerformScientificOperation(string operation, out string oldDisplay, out string oldExpression) {
            oldDisplay = DisplayValue;
            oldExpression = CalculationExpression;
            double input;

            if (!double.TryParse(DisplayValue, out input)) {
                throw new ArgumentException("Bad input!");
            }

            double result = 0;
            string newExpression = "";

            switch (operation) {
                case "π":
                    result = Math.PI;
                    newExpression = "π";
                    break;
                case "e":
                    result = Math.E;
                    newExpression = "e";
                    break;
                case "√":
                    if (input < 0)
                        throw new ArgumentException("sqrt on negative");
                    result = Math.Sqrt(input);
                    newExpression = $"√({input})";
                    break;
                case "n²":
                    result = Math.Pow(input, 2);
                    newExpression = $"({input})²";
                    break;
                case "ln":
                    if (input <= 0)
                        throw new ArgumentException("ln on non-positive");
                    result = Math.Log(input);
                    newExpression = $"ln({input})";
                    break;
                default:
                    //? unreachable
                    throw new ArgumentException("something went wrong");
            }

            DisplayValue = result.ToString();
            CalculationExpression = newExpression;
            IsNewOperation = true;
        }

        public void Calculate(out string oldDisplay, out string oldExpression, 
                              out double firstOperand, out double secondOperand, out string op)
        {
            oldDisplay = DisplayValue;
            oldExpression = CalculationExpression;
            
            string[] parts = CalculationExpression.Split(' ');

            if (!double.TryParse(parts[0], out firstOperand))
                throw new ArgumentException("something went wrong");

            op = parts[1];
            
            if (!double.TryParse(DisplayValue, out secondOperand))
                throw new ArgumentException("something went wrong");

            double result = 0;
            
            switch (op) {
                case "+":
                    result = firstOperand + secondOperand;
                    break;
                case "-":
                    result = firstOperand - secondOperand;
                    break;
                case "*":
                    result = firstOperand * secondOperand;
                    break;
                case "/":
                    if (secondOperand == 0)
                        throw new DivideByZeroException();
                    result = firstOperand / secondOperand;
                    break;
                default:
                    //? unreachable
                    throw new ArgumentException("something went wrong");
            }

            CalculationExpression = $"{CalculationExpression} {DisplayValue} =";
            DisplayValue = result.ToString();
            IsNewOperation = true;
        }
    }

    public interface ICommand {
        void Execute();
        void Undo();
    }

    public class CommandManager {
        private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> redoStack = new Stack<ICommand>();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                ICommand command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                ICommand command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }
    }

    {
        protected readonly CalculatorEngine calculator;
        protected string oldDisplayValue;
        protected string oldCalculationExpression;
        protected bool oldIsNewOperation;

        protected CalculatorCommand(CalculatorEngine calculator)
        {
            this.calculator = calculator;
            oldDisplayValue = calculator.DisplayValue;
            oldCalculationExpression = calculator.CalculationExpression;
            oldIsNewOperation = calculator.IsNewOperation;
        }

        public abstract void Execute();

        public virtual void Undo()
        {
            calculator.SetDisplayValue(oldDisplayValue);
            calculator.SetCalculationExpression(oldCalculationExpression);
            calculator.SetNewOperation(oldIsNewOperation);
        }
    }

    public class InputCommand : CalculatorCommand
    {
        private readonly string number;

        public InputCommand(CalculatorEngine calculator, string number) : base(calculator)
        {
            this.number = number;
        }

        public override void Execute()
        {
            calculator.ProcessNumber(number);
        }
    }

    public class DecimalCommand : CalculatorCommand
    {
        public DecimalCommand(CalculatorEngine calculator) : base(calculator) { }

        public override void Execute()
        {
            calculator.AddDecimalPoint();
        }
    }

    public class BackspaceCommand : CalculatorCommand
    {
        public BackspaceCommand(CalculatorEngine calculator) : base(calculator) { }

        public override void Execute()
        {
            calculator.Backspace();
        }
    }

    public class OperatorCommand : CalculatorCommand
    {
        private readonly string operatorSymbol;

        public OperatorCommand(CalculatorEngine calculator, string operatorSymbol) : base(calculator)
        {
            this.operatorSymbol = operatorSymbol;
        }

        public override void Execute()
        {
            calculator.ApplyOperator(operatorSymbol);
        }
    }

    public class CalculateCommand : CalculatorCommand
    {
        private double firstOperand;
        private double secondOperand;
        private string operatorSymbol;

        public CalculateCommand(CalculatorEngine calculator) : base(calculator) { }

        public override void Execute()
        {
            calculator.Calculate(out _, out _, out firstOperand, out secondOperand, out operatorSymbol);
        }
    }

    public class ScientificCommand : CalculatorCommand
    {
        private readonly string operation;

        public ScientificCommand(CalculatorEngine calculator, string operation) : base(calculator)
        {
            this.operation = operation;
        }

        public override void Execute()
        {
            calculator.PerformScientificOperation(operation, out _, out _);
        }
    }

    public class ClearCommand : CalculatorCommand
    {
        public ClearCommand(CalculatorEngine calculator) : base(calculator) { }

        public override void Execute()
        {
            calculator.Clear();
        }
    }
}