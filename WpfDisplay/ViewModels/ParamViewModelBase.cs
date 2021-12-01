﻿using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public abstract partial class ParamViewModelBase<T>
{
    protected readonly Iterator iterator;
    protected readonly Workspace workspace;

    public string Name { get; protected set; }

    //TODO: min, max, increment, ..
    public ParamViewModelBase(string name, Iterator iterator, Workspace workspace)
    {
        this.Name = name;
        this.iterator = iterator;
        this.workspace = workspace;
    }
}
