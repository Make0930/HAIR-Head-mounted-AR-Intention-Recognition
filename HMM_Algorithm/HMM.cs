using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.IO;
using System;
using System.Data;
using System.Linq;
using UnityEngine.UI;
//using UnityEditor.UI;
using TMPro;

public class HMM : MonoBehaviour
{
    public Camera MRCamera;

    public GameObject Cylinder, Cube, Sphere;
    public GameObject Label,Unknown, Irrational;

    public TextMesh text;
    public Time time;
    //public TextMeshProUGUI text;

    //definite a queue to always keep the newest data
    Queue<int> recent_data = new Queue<int>();
    int index = new int();
    int modified_intention_index = new int();

    public static void add(Queue<int> mQueue, int num1){
          if(mQueue.Count < 5){
              mQueue.Enqueue(num1);
          }
          else{
              mQueue.Dequeue();
              add(mQueue, num1);
          }
        }
    
    

    //int objectnumber = 3;

    Vector3 Lastpoint = new Vector3();

    double[] validation_vector = new double[3];
    double[] validation = new double[3];
    double[] old_validation_vector = new double[3];
    double[] NormalizedB = new double[5];
    //parameters
    static double a = 0.3, beita = 0.1, gama = 0.05, seita = 0.1, g = 3;

    //distance

    float[] d = new float[3];
    float[] D = new float[32];
    float[] scalar_p = new float[3];

    //state transition matrix
    double[,] transition_probability = new double[5, 5] { { 1-a, 0, 0, a, 0 },  { 0, 1 - a,  0, a, 0 }, { 0, 0, 1 - a,  a, 0 },
        { beita, beita, beita, 1-g *beita - gama, gama }, { 0, 0, 0, seita, 1-seita }, };

    //start_state π
    double[] start_state;
    
    //hidden state
    double[] hidden_state = new double[5];
    double[] hidden_state_modified = new double[5];

    double[] B = new double[5];
    double fai;

    int judgement;
    
    // Start is called before the first frame update
    void Start()
    {

        start_state = new double[5] { 0, 0, 0, 1, 0 };
        old_validation_vector = new double[3] { 0, 0, 0 };
        Lastpoint = Vector3.zero;
        judgement = 0;
        
    }

    // Update is called once per frame
    void Update()
    {
        //test = test + 1;
        //text.text = "update to " + test;
        //calculate motion validation vector for each object
        GameObject[] obj = new GameObject[] { Cylinder, Cube, Sphere };
              
        

        Vector3 GazeDirection = CoreServices.InputSystem.GazeProvider.GazeDirection;
        Vector3 GazeOrigin = CoreServices.InputSystem.GazeProvider.GazeOrigin;

        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
            {
                //Debug.Log("We detect the hand source!");
                foreach (var p in source.Pointers)
                {
                    if (p is IMixedRealityNearPointer)
                    {
                        // Ignore near pointers, we only want the rays
                        continue;
                    }

                    // hand position and distance vector
                    if (p.Result != null)
                    {

                        Debug.Log("handposition is " + p.Position.ToString("F8"));
                        //Debug.Log("gazedirection is : " + GazeDirection.normalized);

                        //radius
                        float r = (p.Position - Lastpoint).magnitude;

                        if (r != 0)
                        {
                            ////write time as x asix, before this i use the frame
                            //string path_time = "time.txt";
                            //FileStream fs_time = new FileStream(path_time, FileMode.Append, FileAccess.Write);
                            //using (var sw_time = new StreamWriter(fs_time))
                            //{
                            //    sw_time.WriteLine(Time.time.ToString("F3"));
                            //}
                            //string path_frame ="frame.txt";
                            //FileStream fs_frame = new FileStream(path_frame, FileMode.Append, FileAccess.Write);
                            //using (var sw_frame = new StreamWriter(fs_frame))
                            //{
                            //    sw_frame.WriteLine(judgement);
                            //}
                            ////write hololens

                            //string path_holo = Path.Combine(Application.persistentDataPath, "frame123.txt");
                            //using (TextWriter writer = File.CreateText(path_holo))
                            //{
                            //    // TODO write text here
                            //    writer.WriteLine(judgement);
                            //}


                            for (int i = 0; i < obj.Length; i++)
                            {
                                var dis = (p.Position - obj[i].transform.position).magnitude;
                                d[i] = dis;

                                //calculate D
                                //int count = 16; // 16 equiditant points
                                int count = 32;
                                float radians = ((float)Math.PI / 180) * (float)Math.Round(360.0 / count);


                                for (int j = 0; j < count; j++)
                                {
                                    // can not convert double to float
                                    float x = r * (float)Math.Sin(radians * j) + Lastpoint.x;
                                    float z = r * (float)Math.Cos(radians * j) + Lastpoint.z;
                                    float y = Lastpoint.y;
                                    var p_point = new Vector3(x, y, z);
                                    D[j] = (p_point - obj[i].transform.position).magnitude;

                                }
                                Array.Sort(D);

                                scalar_p[i] = Vector3.Dot((obj[i].transform.position - GazeOrigin).normalized, GazeDirection.normalized);
                                if (scalar_p[i] < 0)
                                    scalar_p[i] = 0;


                                ////write dot_product and distance
                                //if (i == 0)
                                //{
                                //    string path = "dotproduct_Cylinder.txt";
                                //    FileStream fs_dotproduct = new FileStream(path, FileMode.Append, FileAccess.Write);
                                //    //Debug.Log("handposition is " + p.Position);
                                //    using (var sw_dotproduct = new StreamWriter(fs_dotproduct))
                                //    {
                                //        sw_dotproduct.WriteLine(scalar_p);
                                //    }

                                //    string path_distance = "distance_Cylinder.txt";
                                //    FileStream fs_distance = new FileStream(path_distance, FileMode.Append, FileAccess.Write);                                   
                                //    using (var sw_distance = new StreamWriter(fs_distance))
                                //    {
                                //        sw_distance.WriteLine(d[i]);
                                //    }
                                //}
                                //else if (i == 1)
                                //{
                                //    string path = "dotproduct_Cube.txt";
                                //    FileStream fs_dotproduct = new FileStream(path, FileMode.Append, FileAccess.Write);                                 
                                //    using (var sw_dotproduct = new StreamWriter(fs_dotproduct))
                                //    {
                                //        sw_dotproduct.WriteLine(scalar_p);
                                //    }

                                //    string path_distance = "distance_Cube.txt";
                                //    FileStream fs_distance = new FileStream(path_distance, FileMode.Append, FileAccess.Write);
                                //    using (var sw_distance = new StreamWriter(fs_distance))
                                //    {
                                //        sw_distance.WriteLine(d[i]);
                                //    }
                                //}
                                //else
                                //{
                                //    string path = "dotproduct_Sphere.txt";
                                //    FileStream fs_dotproduct = new FileStream(path, FileMode.Append, FileAccess.Write);
                                //    using (var sw_dotproduct = new StreamWriter(fs_dotproduct))
                                //    {
                                //        sw_dotproduct.WriteLine(scalar_p);
                                //    }

                                //    string path_distance = "distance_Sphere.txt";
                                //    FileStream fs_distance = new FileStream(path_distance, FileMode.Append, FileAccess.Write);
                                //    using (var sw_distance = new StreamWriter(fs_distance))
                                //    {
                                //        sw_distance.WriteLine(d[i]);
                                //    }
                                //}
                                validation[i] = (D[31] - d[i]) / (D[31] - D[0]);
                                validation_vector[i] = (D[31] - d[i]) * scalar_p[i] / (D[31] - D[0]);
                            }

                            //consider if the person is rational or irrational.
                            double old_v_average = old_validation_vector.Average();
                            double new_v_average = validation_vector.Average();

                            fai = Math.Max(old_v_average, new_v_average);

                            // the person is rational
                            if (fai > 0.2)
                            {
                                //Calculate delta and B'
                                double[] validation_vector_sorted = new double[validation_vector.Length];
                                Array.Copy(validation_vector, validation_vector_sorted, validation_vector.Length);
                                Array.Sort(validation_vector_sorted);
                                double delta = validation_vector_sorted[2] - validation_vector_sorted[1];


                                B = new double[5]{Math.Tanh(validation_vector[0]), Math.Tanh(validation_vector[1]), Math.Tanh(validation_vector[2]),
                                 Math.Tanh(1-delta), 0};
                            }

                            //the person is irrational
                            else
                            {
                                B = new double[5] { 0, 0, 0, Math.Tanh(0.1), Math.Tanh(1 - fai) };

                            }
                            //Debug.Log("B=( " + B[0].ToString("F3") + " " + B[1].ToString("F3") + " " + B[2].ToString("F3") + " " + B[3].ToString("F3") + " " + B[4].ToString("F3") + " )");


                            //Normalizing B


                            //double[] NormalizedB = new double[B.Length];
                            var zita = 1;
                            for (int i = 0; i < B.Length; i++)
                            {
                                NormalizedB[i] = B[i] * zita;

                            }
                            if (judgement == 0)
                            {
                                for (int j = 0; j < (obj.Length + 2); j++)
                                {
                                    hidden_state[j] = start_state[j] * NormalizedB[j];
                                }
                            }

                            else
                            {
                                for (int j = 0; j < (obj.Length + 2); j++)
                                {
                                    double[] pro = new double[obj.Length + 2];
                                    for (int k = 0; k < (obj.Length + 2); k++)
                                    {
                                        pro[k] = start_state[k] * transition_probability[j, k];
                                    }
                                    hidden_state[j] = pro.Max() * NormalizedB[j];
                                }
                            }


                            //modify the hidden state. let sum of hidden_state equals 1.
                            double probabilitys_sum = 0;
                            for (int i = 0; i < obj.Length + 2; i++)
                                probabilitys_sum = probabilitys_sum + hidden_state[i];

                            for (int i = 0; i < obj.Length + 2; i++)
                            {
                                hidden_state_modified[i] = hidden_state[i] / probabilitys_sum;

                            }

                            double max = hidden_state_modified[0];
                            index = 0;

                            for (int i = 0; i < hidden_state_modified.Length; i++)
                            {
                                if (hidden_state_modified[i] >= max)
                                {
                                    max = hidden_state_modified[i];
                                    index = i;
                                }

                            }
                            //Debug.Log("hidden_state_modified=( " + hidden_state_modified[0].ToString("F8") + " " + hidden_state_modified[1].ToString("F8") +
                            //" " + hidden_state_modified[2].ToString("F8") + " " + hidden_state_modified[3].ToString("F8")
                            //+ " " + hidden_state_modified[4].ToString("F8") + " )");


                            add(recent_data, index);

                            modified_intention_index = (from c in recent_data
                                                        group c by c into g
                                                        orderby g.Count() descending
                                                        select g.Key).FirstOrDefault();

                            //show the result

                            if (index < 3)
                            {
                                //Debug.Log("====================================================================");
                                Debug.Log("max probability is : " + max + " and goal is " + obj[index].name + "\n" + "distance between " + obj[index] + "is" + d[index]);
                                text.text = "Goal is " + obj[index].name + "    " + "probability is :" + max.ToString("F3");
                                // Label.transform.position = obj[index].transform.position;
                                // Label.name = obj[index].name;
                            }

                            else if (index == 3)
                            {
                                //Debug.Log("====================================================================");
                                Debug.Log("max probability is : " + max + " and unknown goal");
                                text.text = "Unknown goal " + "    " + "probability is :" + max.ToString("F3");
                                // Label.transform.position = Unknown.transform.position;
                            }
                            else
                            {
                                //Debug.Log("====================================================================");
                                Debug.Log("max probability is : " + max + " and Irrational worker");
                                text.text = "Irrational worker " + "    " + "probability is :" + max.ToString("F3");
                                // Label.transform.position = Irrational.transform.position;
                            }

                            if (modified_intention_index < 3)
                            {
                                //Debug.Log("====================================================================");
                                //Debug.Log("max probability is : " + max + " and goal is " + obj[index].name + "\n" + "distance between " + obj[index] + "is" + d[index]);
                                Label.transform.position = obj[modified_intention_index].transform.position;
                                Label.name = obj[modified_intention_index].name;
                            }

                            else if (modified_intention_index == 3)
                            {
                                //Debug.Log("====================================================================");
                                //Debug.Log("max probability is : " + max + " and unknown goal");
                                Label.transform.position = Unknown.transform.position;
                            }
                            else
                            {
                                //Debug.Log("====================================================================");
                                //Debug.Log("max probability is : " + max + " and Irrational worker");
                                Label.transform.position = Irrational.transform.position;
                            }

                            Array.Copy(validation_vector, old_validation_vector, validation_vector.Length);
                            Array.Copy(hidden_state_modified, start_state, hidden_state.Length);
                            judgement = judgement + 1;
                            Lastpoint = p.Position;

                        }

                        //string path6 = "intention_index.txt";
                        string path6 = Path.Combine(Application.persistentDataPath, "intention_index.txt");
                        FileStream fs_intention_index = new FileStream(path6, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_intention_index = new StreamWriter(fs_intention_index))
                        {
                            sw_intention_index.WriteLine("{0} {1} {2}", Time.time.ToString("F3"), index.ToString("F3"), modified_intention_index.ToString("F3"));
                        }

                        //string path5 = "recent_data.txt";
                        string path5 = Path.Combine(Application.persistentDataPath, "recent_data.txt");
                        FileStream fs_recent_data = new FileStream(path5, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_recent_data = new StreamWriter(fs_recent_data))
                        {
                            sw_recent_data.Write("{0} ", Time.time.ToString("F3"));
                            foreach (int c in recent_data)
                            {
                                sw_recent_data.Write(c + " ");
                                
                            }
                            sw_recent_data.WriteLine();
                            //sw_recent_data.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), hidden_state_modified[0].ToString("F3"), d[0].ToString("F3"), scalar_p[0].ToString("F3"));
                        }

                        //string path7 = "Hand_move_vector.txt";
                        string path7 = Path.Combine(Application.persistentDataPath, "Hand_move_vector.txt");
                        FileStream fs_Hand_move_vector = new FileStream(path7, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_Hand_move_vectors = new StreamWriter(fs_Hand_move_vector))
                        {
                            sw_Hand_move_vectors.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), validation[0].ToString("F3"), validation[1].ToString("F3"),
                                validation[2].ToString("F3"));
                        }

                        //string path8 = "gaze_vector.txt";
                        string path8 = Path.Combine(Application.persistentDataPath, "gaze_vector.txt");
                        FileStream fs_gaze_vector = new FileStream(path8, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_gaze_vector = new StreamWriter(fs_gaze_vector))
                        {
                            sw_gaze_vector.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), scalar_p[0].ToString("F3"), scalar_p[1].ToString("F3"),
                                scalar_p[2].ToString("F3"));
                        }

                        //string path9 = "validation_vector.txt";
                        string path9 = Path.Combine(Application.persistentDataPath, "validation_vector.txt");
                        FileStream fs_validation_vector = new FileStream(path9, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_validation_vector = new StreamWriter(fs_validation_vector))
                        {
                            sw_validation_vector.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), validation_vector[0].ToString("F3"), validation_vector[1].ToString("F3"),
                                validation_vector[2].ToString("F3"));
                        }

                        //string path10 = "Observation_matrix.txt";
                        string path10 = Path.Combine(Application.persistentDataPath, "Observation_matrix.txt");
                        FileStream fs_Observation_matrix = new FileStream(path10, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_Observation_matrix = new StreamWriter(fs_Observation_matrix))
                        {
                            sw_Observation_matrix.WriteLine("{0} {1} {2} {3} {4} {5}", Time.time.ToString("F3"), NormalizedB[0].ToString("F3"), NormalizedB[1].ToString("F3"),
                                NormalizedB[2].ToString("F3"), NormalizedB[3].ToString("F3"), NormalizedB[4].ToString("F3"));
                        }




                        string path1 = Path.Combine(Application.persistentDataPath, "Cylinder.txt");
                        //string path1 = "Cylinder.txt";
                        FileStream fs_cylinder = new FileStream(path1, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_cylinder = new StreamWriter(fs_cylinder))
                        {
                            sw_cylinder.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), hidden_state_modified[0].ToString("F3"), d[0].ToString("F3"), scalar_p[0].ToString("F3"));
                        }

                        //string path2 = "Cube.txt";
                        string path2 = Path.Combine(Application.persistentDataPath, "Cube.txt");
                        FileStream fs_Cube = new FileStream(path2, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_Cube = new StreamWriter(fs_Cube))
                        {
                            sw_Cube.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), hidden_state_modified[1].ToString("F3"), d[1].ToString("F3"), scalar_p[1].ToString("F3"));
                        }

                        //string path3 = "Sphere.txt";
                        string path3 = Path.Combine(Application.persistentDataPath, "Sphere.txt");
                        FileStream fs_Sphere = new FileStream(path3, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_Sphere = new StreamWriter(fs_Sphere))
                        {
                            sw_Sphere.WriteLine("{0} {1} {2} {3}", Time.time.ToString("F3"), hidden_state_modified[2].ToString("F3"), d[2].ToString("F3"), scalar_p[2].ToString("F3"));
                        }

                        //string path4 = "Probablities.txt";
                        string path4 = Path.Combine(Application.persistentDataPath, "Probablities.txt");
                        FileStream fs_Probablities = new FileStream(path4, FileMode.Append, FileAccess.Write);
                        using (StreamWriter sw_Probablities = new StreamWriter(fs_Probablities))
                        {
                            sw_Probablities.WriteLine("{0} {1} {2} {3} {4} {5}", Time.time.ToString("F3"), hidden_state_modified[0].ToString("F3"), hidden_state_modified[1].ToString("F3"),
                                hidden_state_modified[2].ToString("F3"), hidden_state_modified[3].ToString("F3"), hidden_state_modified[4].ToString("F3"));
                        }
                    }
                }
            }
        }
    }
}
