import React, { ChangeEventHandler } from "react";
import {   useImperativeHandle, useState } from "react";

 
export function Input<Type extends string | number | readonly string[] | null>(required: boolean | undefined, id: string | undefined, name: string ,type: string, step: string| undefined,  placeholder: string, internalValue: Type, handleOnChange:   ChangeEventHandler<HTMLInputElement>,className:string
    |undefined
): React.ReactNode {
    return <input
        required={required}
        id={id?.toString()}
        name={name}
        type={type} 
        step={step}
        className={className}
        placeholder={placeholder}
        value={internalValue??''}
        onChange={handleOnChange} />;
}

export type EditableCellHandle<T> = {
    getValue: () => T | null;
    setValue(value:T | null):void
  };
  
export interface IEditableBaseCellProps<Type extends string | number  | readonly string[] | null>  {
    defaultValue:Type, 
    placeholder:string,   
    id?:string| undefined 
    name:string, 
    isEditing:boolean,  
    className?: string | undefined 
    required? :boolean| undefined
 };
 export interface IEditableNumericCellProps<Type extends   number | null> extends IEditableBaseCellProps<Type> { 
    isMoney?:boolean | undefined, 
    isPercentage?:boolean| undefined,
    step: string 
 };

 const EditableTextCell = React.forwardRef<EditableCellHandle<string>,IEditableBaseCellProps<string|null>>((props, ref)  => {
    const { 
        defaultValue, 
        placeholder, 
        id, 
        name,
        className,
        isEditing,  
        required 
    } = props;

    const [internalValue, setInternalValue] = useState(defaultValue);

 
    useImperativeHandle(ref, () => ({
        getValue():string | null {
            return internalValue;
        },
        setValue(value:string | null) {
            return setInternalValue(value);
        },
    }));
  
    if (!isEditing) return internalValue;
 
  
    return Input(required, id,name, "text", undefined, placeholder, internalValue, (e)=>setInternalValue(e.currentTarget.value),className);
 
})

EditableTextCell.displayName = 'EditableTextCell'
export {
    EditableTextCell
}
 


 
