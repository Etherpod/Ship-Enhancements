using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class PriorityScreenPrompt : ScreenPrompt
{
    public PriorityScreenPrompt(string prompt, int priority = 0) : base(prompt, priority)
    {
        
    }

    // Token: 0x06000C66 RID: 3174 RVA: 0x0000BAE4 File Offset: 0x00009CE4
    public PriorityScreenPrompt(string prompt, Sprite customSprite, int priority = 0) : base(prompt, customSprite, priority)
    {
        
    }

    // Token: 0x06000C67 RID: 3175 RVA: 0x00067268 File Offset: 0x00065468
    public PriorityScreenPrompt(IInputCommands cmd, string prompt, int priority = 0, ScreenPrompt.DisplayState displayState = ScreenPrompt.DisplayState.Normal, bool overridePriority = false)
        : base(cmd, prompt, priority, displayState, overridePriority)
    {
        
    }

    // Token: 0x06000C68 RID: 3176 RVA: 0x000672EC File Offset: 0x000654EC
    public PriorityScreenPrompt(IInputCommands cmd, IInputCommands cmd2, string prompt, ScreenPrompt.MultiCommandType multiCmdType, int priority = 0, ScreenPrompt.DisplayState displayState = ScreenPrompt.DisplayState.Normal, bool overridePriority = false)
        : base(cmd, cmd2, prompt, multiCmdType, priority, displayState, overridePriority)
    {
        
    }

    // Token: 0x06000C69 RID: 3177 RVA: 0x00067360 File Offset: 0x00065560
    public PriorityScreenPrompt(List<IInputCommands> cmdList, string prompt, ScreenPrompt.MultiCommandType multiCmdType, int priority = 0, ScreenPrompt.DisplayState displayState = ScreenPrompt.DisplayState.Normal, bool overridePriority = false)
        : base(cmdList, prompt, multiCmdType, priority, displayState, overridePriority)
    {
        
    }

    // Token: 0x06000C6A RID: 3178 RVA: 0x000673DC File Offset: 0x000655DC
    ~PriorityScreenPrompt()
    {
    }
}
