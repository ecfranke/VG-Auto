import { IProduct } from "../../model";
import { DataItemRowHandle } from "../editabletable/DataIItemRow";

export function dragAndDrop(
     refreshData: (data: IProduct[]) => void, 
     rowRef: React.RefObject<DataItemRowHandle<IProduct>[] | null[]>,
     dragItem: React.RefObject<string | null | undefined>,
     dragOverItem: React.RefObject<string | null | undefined>) {

    
    const handleDragStart = (e: React.DragEvent<HTMLTableRowElement>) => {

        dragItem.current = e.currentTarget.id;
    }
    const handleDragEnter = (e: React.DragEvent<HTMLTableRowElement>) => {

        dragOverItem.current = e.currentTarget.id;
        drop();
    }

    const collectValues = () => {
        const vals = rowRef.current.map(r => {
            return r?.getValue();
        }).filter(x => x !== null);

        return vals as IProduct[];
    }

    const drop = () => {
        const copyListItems = collectValues();
        if (!dragItem.current || !dragOverItem.current) return;
        const currentDraggedItemId = dragItem.current
        const overItemId = dragOverItem.current
        const dragItemContent = copyListItems.find(x => x.id == currentDraggedItemId);
        if (!dragItemContent) return;
        const currentIndex = copyListItems.findIndex(x => x.id == currentDraggedItemId);
        const newIndex = copyListItems.findIndex(x => x.id == overItemId);
        copyListItems.splice(currentIndex, 1);
        copyListItems.splice(newIndex, 0, dragItemContent);
        //dragItem.current = null;
        //dragOverItem.current = null; 
        refreshData(copyListItems);
    }

    return {
        handleDragStart,
        handleDragEnter
    }
}