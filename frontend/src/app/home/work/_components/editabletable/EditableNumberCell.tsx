import React, { useState, useImperativeHandle } from "react";
import { EditableCellHandle, IEditableNumericCellProps, Input } from "./EditableCell";
import { formatMoney } from "@/_lib/shared/money";
import { useCurrency } from "../CurrencyContext";

const EditableNumberCell = React.forwardRef<EditableCellHandle<number|null>, IEditableNumericCellProps<number|null>>((props, ref) => {
    const {
        defaultValue, placeholder, id, name, isEditing, className, step, isMoney, isPercentage, required
    } = props;

    const [internalValue, setInternalValue] = useState(defaultValue);
    const currency = useCurrency();

    useImperativeHandle(ref, () => ({
        getValue(): number | null{
            return internalValue;
        },
        setValue(value: number |  null) {
            return setInternalValue(value);
        },
    }));

    const getFormattedValue = () => {
        if (internalValue === 0) return '';
        if (isMoney) {
            if (!internalValue) return '';
            return formatMoney(internalValue, currency);
        }
        if (isPercentage) return internalValue + ' %';
        return internalValue;
    };
    if (!isEditing) return getFormattedValue();

    return Input(required, id, name, "number", step, placeholder, internalValue, (e) => setInternalValue(+e.currentTarget.value), className);

});
EditableNumberCell.displayName = "EditableNumberCell";
export {
    EditableNumberCell
}