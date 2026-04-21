using System;

public abstract class DecisionNode
{
    public abstract DecisionNode Evaluate();
}
public class ActionNode : DecisionNode
{
    private readonly Action action;

    public ActionNode(Action action) => this.action = action;

    public override DecisionNode Evaluate() => this;

    public void Execute() => action?.Invoke();
}
public class ConditionNode : DecisionNode
{
    private readonly Func<bool> condition;
    private readonly DecisionNode trueNode;
    private readonly DecisionNode falseNode;

    public ConditionNode(Func<bool> condition, DecisionNode trueNode, DecisionNode falseNode)
    {
        this.condition = condition;
        this.trueNode = trueNode;
        this.falseNode = falseNode;
    }

    public override DecisionNode Evaluate()
        => condition() ? trueNode.Evaluate() : falseNode.Evaluate();
}
public class DecisionTree
{
    private readonly DecisionNode root;

    public DecisionTree(DecisionNode root) => this.root = root;

    public void Execute()
    {
        if (root.Evaluate() is ActionNode action)
            action.Execute();
    }
}