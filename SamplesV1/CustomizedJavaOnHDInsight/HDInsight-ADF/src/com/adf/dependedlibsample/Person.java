package com.adf.dependedlibsample;

public class Person
{
	public Person(String name, String phoneNumber)
	{
		this.name = name;
		this.phoneNumber = phoneNumber;
	}

	public String GetName()
	{
		return name;
	}

	public String getPhoneNumber()
	{
		return phoneNumber;
	}
	
	private String name;
	private String phoneNumber;
}
