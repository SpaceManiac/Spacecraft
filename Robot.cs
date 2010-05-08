
using System;
using System.Collections;

namespace spacecraft
{
    public class Robot : Player
    {
        private double time;

        public Robot(string name)
            : base(name)
        {
            time = 0;
        }

        public void Update()
        {
            MinecraftServer serv = MinecraftServer.theServ;

            time += (1.0 / 30);
            if (time < 1)
            {
                x += 6;
            }
            else if (time < 2)
            {
                z += 6;
            }
            else if (time < 3)
            {
                x -= 6;
            }
            else if (time < 4)
            {
                z -= 6;
            }
            else
            {
                time = 0;
                //Update(serv);
            }
            serv.SendAll(Connection.PacketPositionUpdate(this));
        }
    }
}