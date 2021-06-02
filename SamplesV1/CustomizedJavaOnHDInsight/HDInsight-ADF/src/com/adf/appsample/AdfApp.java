package com.adf.appsample;

import java.util.List;
import java.util.ArrayList;

import com.adf.dependedlibsample.Person;

public class AdfApp
{
	public static void main(String[] args)
	{
		List<Person> personList = new ArrayList<Person>();
		
		personList.add(new Person("Alan", "425-123-4567"));
		personList.add(new Person("Angela", "425-987-6543"));
		
		for (Person p : personList)
		{
			System.out.println(p.GetName() + " : " + p.getPhoneNumber());
		}
	}
}