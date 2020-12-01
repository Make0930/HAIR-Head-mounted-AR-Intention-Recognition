#!/usr/bin/env python
# -*- coding: utf-8 -*-

import rospy, sys
import moveit_commander
from geometry_msgs.msg import PoseStamped, Pose
import thread, copy
from moveit_commander import RobotCommander, MoveGroupCommander, PlanningSceneInterface, roscpp_initialize
from moveit_msgs.msg import CollisionObject, AttachedCollisionObject, PlanningScene, ObjectColor
from math import radians
from copy import deepcopy
import moveit_msgs.msg
import geometry_msgs.msg
from math import pi
from std_msgs.msg import String
from moveit_commander.conversions import pose_to_list
import random
import threading


def thread_job():
    rospy.spin()

def listener():

    global label
    #rospy.init_node('pos', anonymous=True)
    data = rospy.wait_for_message("target_position",PoseStamped,timeout = None)
    #rospy.Subscriber('target_position', PoseStamped, callback,queue_size=1,buff_size=52428800) #TOPIC

    label= data.pose.position.y
    #print("=======laqbel is:======", data.pose.position.y)    
    #add_thread = threading.Thread(target = thread_job)
    #add_thread.start()

#def callback(data):
    #global label
   # label= data.pose.position.y

class MoveItIkDemo:
    def __init__(self):
        #global a=False
        
        # remap again
        #joint_state_topic = ['joint_states:=/robot/joint_states']
        #moveit_commander.roscpp_initialize(joint_state_topic)
        
        # 初始化ROS节点
        rospy.init_node('moveit_ik')
        moveit_commander.roscpp_initialize(sys.argv)

         # 初始化场景对象
        scene = PlanningSceneInterface()
        self.scene_pub = rospy.Publisher('planning_scene', PlanningScene)
        rospy.sleep(1)

        # # 设置场景物体的名称
        # table_id = 'table'
        # box_id = 'box'
        # cylinder_id = 'cylinder'
                
        # 初始化需要使用move group控制的机械臂中的arm group
        arm = moveit_commander.MoveGroupCommander('arm')
        
        #print arm.get_current_pose()
                
        # 获取终端link的名称
        end_effector_link = arm.get_end_effector_link()
                        
        # 设置目标位置所使用的参考坐标系
        reference_frame = 'base_link'
        arm.set_pose_reference_frame(reference_frame)

               
        # 当运动规划失败后，允许重新规划
        arm.allow_replanning(True)
        
        # 设置位置(单位：米)和姿态（单位：弧度）的允许误差
        arm.set_goal_position_tolerance(0.01)
        arm.set_goal_orientation_tolerance(0.01)
       
        # 设置允许的最大速度和加速度
        arm.set_max_acceleration_scaling_factor(0.03)
        arm.set_max_velocity_scaling_factor(0.03)
        self.colors = dict()
        # # 控制机械臂先回到初始化位置
        # arm.set_named_target('home')
        # arm.go()
        # rospy.sleep(1)

        # 移除场景中之前运行残留的物体
        #scene.remove_attached_object(end_effector_link, 'tool')
        #scene.remove_world_object('box1')
        #scene.remove_world_object('box2')
        #scene.remove_world_object('box3')

        # the table height, which the object was put on
        table_height = 0.27
        
        # 设置table和tool的三维尺寸
        # table_size = [0.1, 0.7, 0.01]
        # tool_size = [0.2, 0.02, 0.02]
        box_size = [0.05,0.05,0.05]

        #add boxs to the scene
        box1_pose = PoseStamped()
        #set robot_baselink as the the reference koordinate 
        box1_pose.header.frame_id = 'base_link'
        box1_pose.pose.position.x = 1.15
        box1_pose.pose.position.y = -0.766
        box1_pose.pose.position.z =table_height + box_size[2] / 2.0
        box1_pose.pose.orientation.w = 1.0
        
        box2_pose = PoseStamped()
        #set robot_baselink as the the reference koordinate 
        box2_pose.header.frame_id = 'base_link'
        box2_pose.pose.position.x = 0.916
        box2_pose.pose.position.y = -0.2
        box2_pose.pose.position.z =table_height + box_size[2] / 2.0
        box2_pose.pose.orientation.w = 1.0
        
        box3_pose = PoseStamped()
        #set robot_baselink as the the reference koordinate 
        box3_pose.header.frame_id = 'base_link'
        box3_pose.pose.position.x = 1.15
        box3_pose.pose.position.y = 0.366
        box3_pose.pose.position.z =table_height + box_size[2] / 2.0
        box3_pose.pose.orientation.w = 1.0



        scene.add_box('box1',box1_pose,box_size)
        scene.add_box('box2',box2_pose,box_size)        
        scene.add_box('box3',box3_pose,box_size)
        
        self.setColor('box2', 0.8, 0, 0, 1.0)
        self.setColor('box3', 0, 0, 0.8, 1.0)
        self.sendColors()        

        pose_goal1 = geometry_msgs.msg.Pose()
        pose_goal1.orientation.x = 0
        pose_goal1.orientation.y = 1
        pose_goal1.orientation.z = 0
        pose_goal1.orientation.w = 0
        pose_goal1.position.x = 1.15
        pose_goal1.position.y = -0.766
        pose_goal1.position.z = 0.4

        pose_goal2 = geometry_msgs.msg.Pose()
        pose_goal2.orientation.x = 0
        pose_goal2.orientation.y = 1
        pose_goal2.orientation.z = 0
        pose_goal2.orientation.w = 0
        pose_goal2.position.x =0.916
        pose_goal2.position.y = -0.2
        pose_goal2.position.z = 0.4
        
        pose_goal3 = geometry_msgs.msg.Pose()
        pose_goal3.orientation.x = 0
        pose_goal3.orientation.y = 1
        pose_goal3.orientation.z = 0
        pose_goal3.orientation.w = 0
        pose_goal3.position.x =1.15
        pose_goal3.position.y = 0.366
        pose_goal3.position.z = 0.4
        
        
        #initial the check times
        global t1
        global t2
        global t3
        t1 = 0
        t2 = 0
        t3 = 0
        #check if the current target is the same
        def check_goal_1():
            global t1
            if t1 == 12:
                
                plan = arm.go(wait=True)
                print "Robot get the cylinder"
                return 0
            rospy.sleep(1)
            listener()
            #print "the current {} label is".format(t1),label
            if  abs(label + 0.707) <=0.1:
                print "Stop! The human intention changed to the cylinder!"
                rospy.sleep(0.05)
                #arm.stop()
                #arm.clear_pose_targets()
                return 0
            else:
                t1 +=1
                #print arm.get_current_pose().pose.position
                check_goal_1()
        
        def check_goal_2():
            global t2
            if t2 == 7:
                plan = arm.go(wait=True)
                print "Robot get the cube"
                return 0
            rospy.sleep(1)
            listener()
            #print "the current {} label is".format(t2),label
            if  abs(label + 0) <=0.1:
                print "Stop! The human intention changed to the cube!"
                rospy.sleep(0.05)
                #arm.stop()
                #arm.clear_pose_targets()
                return 0
            else:
                t2 +=1
                #print arm.get_current_pose().pose.position
                check_goal_2()

        def check_goal_3():
            global t3
            if t3 == 8:
                plan = arm.go(wait=True)
                print "Robot get the sphere"
                return 0
            rospy.sleep(1)
            listener()
            #print "the current {} label is".format(t3),label
            if  abs(label - 0.707) <=0.1:
                print "Stop! The human intention changed to the sphere!"
                rospy.sleep(0.05)
                #arm.stop()
                #arm.clear_pose_targets()
                return 0
            else:
                t3 +=1
                #print arm.get_current_pose().pose.position
                check_goal_3()
        

        def move_to_1():
            
            global t1
            arm.set_pose_target(pose_goal1)
            plan = arm.go(wait=False)
            check_goal_1()
            t1 = 0
                    
        def move_to_2():
            global t2
            arm.set_pose_target(pose_goal2)
            plan = arm.go(wait=False)
            check_goal_2()
            t2 = 0
                    
        def move_to_3():
            global t3
            arm.set_pose_target(pose_goal3)
            plan = arm.go(wait=False)
            check_goal_3()
            t3 = 0
                
        def move():
            arm.set_named_target('home')   
            arm.go()
            rospy.sleep(1)

            target = random.randint(1,3)
            #target = 2
            if target == 1:           # the roboter intention is cylinder
                
                listener()
                
                if abs(label + 0.707) <=0.1:  
                    # the human intention is cylinder      stop 
                    print "Robot plan to pick cylinder"
                    print "But the human intention is the cylinder!"
                    print "Stop to pick cylinder!"
                    #arm.stop()
                    arm.set_named_target('home')
                    plan = arm.go(wait=True)
 
                        #arm.clear_pose_targets()
                elif abs(label + 0) <=0.1:
                    print "Human plan to pick cube, Robot can not pick the same object!"
                    print "Robot plan to pick cylinder"
                    move_to_1()
                elif abs(label - 0.707) <=0.1:
                    print "Human plan to pick sphere, Robot can not pick the same object!"
                    print "Robot plan to pick cylinder"
                    move_to_1()
                elif abs(label - 4 ) <= 0.1:
                    print "the Human intention is unknown"
                    print "Robot plan to pick cylinder"
                    move_to_1()
                else:
                    print "the Human intention is irrational"
                    print "Robot plan to pick cylinder"
                    move_to_1()
                #while(listener()== None):
                    #arm.set_pose_target(pose_goal1)
                    #plan = arm.go(wait=True)
                    #arm.stop()
                    #arm.clear_pose_targets()
                    #c = 1
                   # break
            elif target == 2:           # the roboter intention is cube
                
                listener()

                if abs(label + 0) <=0.1:       # the human intention is cube      stop 
                    print "Robot plan to pick cube"
                    print "But the human intention is the cube!"
                    print "Stop to pick cube!"
                    #arm.stop()
                    arm.set_named_target('home')
                    plan = arm.go(wait=True)
                elif abs(label + 0.707) <=0.1:
                    print "Human plan to pick cylinder, Robot can not pick the same object!"
                    print "Robot plan to pick cube"
                    move_to_2()
                elif abs(label - 0.707) <=0.1:
                    print "Human plan to pick sphere, Robot can not pick the same object!"
                    print "Robot plan to pick cube"
                    move_to_2()
                elif abs(label - 4 ) <= 0.1:
                    print "the Human intention is unknown"
                    print "Robot plan to pick cube"
                    move_to_2()
                else:
                    print "the Human intention is irrational"
                    print "Robot plan to pick cube"
                    move_to_2()
                

            elif target == 3:               # the roboter intention is sphere
                
                listener()

                if abs(label - 0.707) <=0.1:       # the human intention is sphere      stop 
                    print "Robot plan to pick sphere"
                    print "But the human intention is the sphere!"
                    print "Stop to pick sphere!"
                    #arm.stop()
                    arm.set_named_target('home')
                    plan = arm.go(wait=True)
                elif abs(label + 0.707) <=0.1:
                    print "Human plan to pick cylinder, Robot can not pick the same object!"
                    print "Robot plan to pick sphere"
                    move_to_3()
                elif abs(label + 0) <=0.1:
                    print "Human plan to pick cube, Robot can not pick the same object!"
                    print "Robot plan to pick sphere"
                    move_to_3()
                elif abs(label - 4 ) <= 0.1:
                    print "the Human intention is unknown"
                    print "Robot plan to pick sphere"
                    move_to_3()
                else:
                    print "the Human intention is irrational"
                    print "Robot plan to pick sphere"
                    move_to_3()
            

            
            print "====================================="
            print "====================================="
            move()

        move()

        # 控制机械臂回到初始化位置
        arm.set_named_target('home')
        arm.go()
        

        moveit_commander.roscpp_shutdown()
        moveit_commander.os._exit(0)
    def setColor(self, name, r, g, b, a = 0.9):
        # Initialize a MoveIt color object
        color = ObjectColor()

        # Set the id to the name given as an argument
        color.id = name

        # Set the rgb and alpha values given as input
        color.color.r = r
        color.color.g = g
        color.color.b = b
        color.color.a = a

        # Update the global color dictionary
        self.colors[name] = color
        
    def sendColors(self):
        # Initialize a planning scene object
        p = PlanningScene()

        # Need to publish a planning scene diff        
        p.is_diff = True

        # Append the colors from the global color dictionary 
        for color in self.colors.values():
            p.object_colors.append(color)

        # Publish the scene diff
        self.scene_pub.publish(p)


if __name__ == '__main__':

      MoveItIkDemo()
 
