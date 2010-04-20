
using System;
using System.Collections;

public class Robot : Player
{
    public Robot(string name)
        : base(name)
    {
        // ...
    }
    
    public void Update()
    {
        x += 1;
    }
}
