package com.adf.spark;

import java.io.InputStream;

/**
 * Created by nishantha.pelendage on 16/11/2015.
 */
public class SparkException extends Exception {
    private InputStream err;
    private int exitCode;

    public SparkException(){
        super();
    }

    public SparkException(String message,int exitCode,InputStream err){
        super(message);
        this.err = err;
        this.exitCode = exitCode;
    }


    public InputStream getErr() {
        return err;
    }

    public void setErr(InputStream err) {
        this.err = err;
    }

    public int getExitCode() {
        return exitCode;
    }

    public void setExitCode(int exitCode) {
        this.exitCode = exitCode;
    }
}
