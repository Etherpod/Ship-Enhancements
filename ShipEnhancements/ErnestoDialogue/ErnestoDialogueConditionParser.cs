using Antlr4.Runtime;

namespace ShipEnhancements.ErnestoDialogue;

public class ErnestoDialogueConditionParser
{
	public static void P()
	{
		var ais = new AntlrInputStream("(S:Temperature<=5) && C:VISITED_GD==true");
		var lexer = new EDCLexer(ais);
		var tokens = new CommonTokenStream(lexer);
		var parser = new EDCParser(tokens);
		
		var visitor = new ErnestoDialogueConditionVisitor();
		var conditionContext = parser.condition();
		var parsedCondition = visitor.Visit(conditionContext);
	}
}