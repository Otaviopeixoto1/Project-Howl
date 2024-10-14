public static class MSTables
{
    //16 possible cases (2^4)
    public static uint[] caseToNumLines = 
    {
        0, 1, 1, 1,
        1, 2, 1, 1,
        1, 1, 2, 1,
        1, 1, 1, 0
    };
    public static int [] edgeConnectList =
    {
        //int2 line[i][0] & int2 line[i][1] (up to two lines are generated per cell)
        -1, -1,  -1, -1, 
         0,  3,  -1, -1, 
         0,  1,  -1, -1,
         1,  3,  -1, -1,
         1,  2,  -1, -1,
         0,  1,   2,  3,
         0,  2,  -1, -1,
         2,  3,  -1, -1,
         2,  3,  -1, -1,
         0,  2,  -1, -1,
         0,  3,   1,  2,
         1,  2,  -1, -1,
         1,  3,  -1, -1,
         0,  1,  -1, -1,
         0,  3,  -1, -1,
        -1, -1,  -1, -1,
    };


    public class EdgeLUTs 
    {
        /*
        public float4[4] edge_start;
        public float4[4] edge_dir;
        public float4[4] edge_end;
        public uint[4] edge_axis;  // 0 for x edges, 1 for y edges
        */

        public static int Size()
        {
            return   4*sizeof(float) * 4             
                    + 4*sizeof(float) * 4
                    + 4*sizeof(float) * 4
                    + 4 *sizeof(float) * 4;
        }
        public static float[] GetEdgeLUTs()
        {
            float[] luts = new float[64]
            {
                //float4 edge_start[4] = 
                0, 0, 0, 0,  0, 1, 0, 0,  1, 0, 0, 0,  0, 0, 0, 0, 

                //float4 edge_dir[4] = 
                0, 1, 0, 0,  1, 0, 0, 0,  0, 1, 0, 0,  1, 0, 0, 0, 


                //float4 edge_end[4] = 
                0, 1, 0, 0,  1, 1, 0, 0,  1, 1, 0, 0,  1, 0, 0, 0, 

                //float4 edge_axis[4] =
                1, 0, 0, 0,  0, 0, 0, 0,  1, 0, 0, 0,  0, 0, 0, 0, 
            };

            return luts;
        }
    };
    


}