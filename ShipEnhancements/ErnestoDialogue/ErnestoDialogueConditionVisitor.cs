namespace ShipEnhancements.ErnestoDialogue;

public class ErnestoDialogueConditionVisitor : EDCBaseVisitor<object>
{
	public override object VisitComplex_condition(EDCParser.Complex_conditionContext context)
	{
		// something. idk. haven't figured out what goes here.
		return base.VisitComplex_condition(context);
	}

	public override object VisitSimple_condition(EDCParser.Simple_conditionContext context)
	{
		var lhs = context.value(0);
		var rhs = context.value(1);
		var cond = context.cond();

		var leftValue = lhs.Accept(this);
		var rightValue = rhs.Accept(this);
		
		// compare the values

		return null;
	}

	public override object VisitSetting_name(EDCParser.Setting_nameContext context)
	{
		var settingName = context.GetText().Substring(2);
		
		// get the setting value and return that
		
		return null;
	}

	public override object VisitPersistent_condition_name(EDCParser.Persistent_condition_nameContext context)
	{
		var conditionName = context.GetText().Substring(2);
		
		// get the condition value and return that
		
		return null;
	}
}